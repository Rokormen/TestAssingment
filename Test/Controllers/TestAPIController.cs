using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Text;
using Test.Models;
using Test.Modules;

namespace Test.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestAPIController : ControllerBase
    {
        PostgreSQLConn conn = new PostgreSQLConn();

        [HttpPost(Name = "ParseCSV")]
        public string ParseCSV(IFormFile file)
        {
            try
            {
                if (!file.FileName.EndsWith(".csv"))
                {
                    return "Not right format of file";
                }

                if (file == null || file.Length == 0)
                {
                    //Abort
                    return "File is not found or empty";
                }

                FirstCallFunc magic = new FirstCallFunc();
                string res;

                float MedVal = 0;
                string FileName = file.FileName;

                List<string> TempLines;
                List<CSVValues> DBentries = new List<CSVValues>();

                magic.ReadCSVStream(file, out TempLines);

                if (TempLines.Count < 1 || TempLines.Count > 10000)
                {
                    //Abort
                    return "File contains incorrect number of rows";
                }

                if (!(res = magic.ParseCSV(TempLines, FileName, out DBentries)).Equals("0"))
                {
                    return res;
                }

                //SaveToDB
                using (NpgsqlConnection dbconn = conn.CreateConnection())
                {
                    dbconn.Open();

                    using (NpgsqlCommand cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = dbconn;

                        cmd.CommandText = "SELECT * FROM values WHERE filename = @var1 LIMIT 1";
                        cmd.Parameters.AddWithValue("@var1", FileName);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                reader.Close();
                                cmd.CommandText = "DELETE FROM values WHERE filename = @var1; DELETE FROM results WHERE filename = @var1;";
                                cmd.ExecuteNonQuery();
                            }
                        }
                        cmd.Parameters.Clear();


                        StringBuilder sql = new StringBuilder();
                        int batchsize = 0;

                        for (int i = 0; i < DBentries.Count; i++)
                        {
                            string pDate = string.Format("@p{0}", i * 4);
                            string pExTime = string.Format("@p{0}", i * 4 + 1);
                            string pVal = string.Format("@p{0}", i * 4 + 2);
                            string pFileN = string.Format("@p{0}", i * 4 + 3);

                            string row = string.Format("({0},{1},{2},{3})", pDate, pExTime, pVal, pFileN);

                            if (batchsize > 0)
                            {
                                sql.Append(',');
                            }
                            sql.Append(row);
                            batchsize++;

                            cmd.Parameters.AddWithValue(pDate, DBentries[i].Date);
                            cmd.Parameters.AddWithValue(pExTime, DBentries[i].ExecutionTime);
                            cmd.Parameters.AddWithValue(pVal, DBentries[i].Value);
                            cmd.Parameters.AddWithValue(pFileN, DBentries[i].FileName);

                            if (batchsize >= 20)
                            {                                
                                cmd.CommandText = "INSERT INTO values (date, executiontime, value, filename) VALUES " +
                                    sql.ToString();
                                cmd.ExecuteNonQuery();
                                cmd.Parameters.Clear();
                                batchsize = 0;
                            }

                        }

                        if (batchsize > 0)
                        {
                            cmd.CommandText = "INSERT INTO values (date, executiontime, value, filename) VALUES " +
                                sql.ToString();
                            cmd.ExecuteNonQuery();
                        }

                    }

                    using (NpgsqlCommand cmd = new NpgsqlCommand())
                    {
                        cmd.Connection = dbconn;

                        //Calculations

                        ResultsValues val = magic.CalculateResultsExptMedVal(DBentries.Count);

                        if (DBentries.Count % 2 == 0)
                        {
                            cmd.CommandText = "SELECT value FROM values WHERE filename = @var1 ORDER BY value ASC LIMIT 2 OFFSET @var2";
                            cmd.Parameters.AddWithValue("@var1", FileName);
                            cmd.Parameters.AddWithValue("@var2", DBentries.Count / 2 - 1);
                            using (NpgsqlDataReader reader = cmd.ExecuteReader())
                                while (reader.Read())
                                {
                                    MedVal += reader.GetFieldValue<float>(0);
                                }
                            MedVal /= 2;
                        }
                        else
                        {
                            cmd.CommandText = "SELECT value FROM values WHERE filename = @var1 ORDER BY value ASC LIMIT 1 OFFSET @var2";
                            cmd.Parameters.AddWithValue("@var1", FileName);
                            cmd.Parameters.AddWithValue("@var2", DBentries.Count / 2);
                            using (NpgsqlDataReader reader = cmd.ExecuteReader())
                            {
                                reader.Read();
                                MedVal = reader.GetFieldValue<float>(0);
                            }
                        }

                        cmd.Parameters.Clear();

                        cmd.CommandText = "INSERT INTO results (ddate, mindate, avgextime, avgval, medval, maxval, minval, filename) VALUES (@var1, @var2, @var3, @var4, @var5, @var6, @var7, @var8)";
                        cmd.Parameters.AddWithValue("@var1", val.ddate);
                        cmd.Parameters.AddWithValue("@var2", val.mindate);
                        cmd.Parameters.AddWithValue("@var3", val.avgextime);
                        cmd.Parameters.AddWithValue("@var4", val.avgval);
                        cmd.Parameters.AddWithValue("@var5", MedVal);
                        cmd.Parameters.AddWithValue("@var6", val.maxval);
                        cmd.Parameters.AddWithValue("@var7", val.minval);
                        cmd.Parameters.AddWithValue("@var8", val.filename);

                        cmd.ExecuteNonQuery();

                    }                    
                }
                return "Values inserted";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "Error happened during execution. Most likely cause is PostgeSQL database connection issues.";
            }
        }

        [HttpGet("ShowResults", Name = "ShowResults")]
        public ResultTuple ShowResults([FromQuery(Name = "filter")] string filter, [FromQuery(Name = "start")] string start, [FromQuery(Name = "end")] string end = "end")
        {
            try
            {
                List<ResultsValues> values = new List<ResultsValues>();
                switch (filter)
                {
                    case "filename":
                        using (NpgsqlConnection dbconn = conn.CreateConnection())
                        {
                            dbconn.Open();
                            using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM results WHERE filename = @var1", dbconn))
                            {
                                cmd.Parameters.AddWithValue("@var1", start);
                                using (NpgsqlDataReader reader = cmd.ExecuteReader())
                                {

                                    while (reader.Read())
                                    {
                                        values.Add(new ResultsValues(reader.GetFieldValue<int>(0), reader.GetFieldValue<DateTime>(1), reader.GetFieldValue<float>(2), reader.GetFieldValue<float>(3), reader.GetFieldValue<float>(4), reader.GetFieldValue<float>(5), reader.GetFieldValue<float>(6), reader.GetFieldValue<string>(7)));
                                    }
                                }
                            }
                        }
                        return new ResultTuple(values, "success");
                    case "opstart":
                        if (DateTime.TryParse(start, out DateTime MinDate) &&
                            DateTime.TryParse(end, out DateTime MaxDate))
                        {
                            using (NpgsqlConnection dbconn = conn.CreateConnection())
                            {
                                dbconn.Open();
                                using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM results WHERE @var1 <= mindate AND mindate < @var2", dbconn))
                                {
                                    cmd.Parameters.AddWithValue("@var1", MinDate);
                                    cmd.Parameters.AddWithValue("@var2", MaxDate);
                                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                                    {

                                        while (reader.Read())
                                        {
                                            values.Add(new ResultsValues(reader.GetFieldValue<int>(0), reader.GetFieldValue<DateTime>(1), reader.GetFieldValue<float>(2), reader.GetFieldValue<float>(3), reader.GetFieldValue<float>(4), reader.GetFieldValue<float>(5), reader.GetFieldValue<float>(6), reader.GetFieldValue<string>(7)));
                                        }
                                    }
                                }
                            }

                            return new ResultTuple(values, "success");
                        }
                        else
                        {
                            return new ResultTuple(null, "Times set incorrectly");
                        }
                    case "avgval":
                        if (float.TryParse(start, out float MinValue) &&
                            float.TryParse(end, out float MaxValue))
                        {
                            using (NpgsqlConnection dbconn = conn.CreateConnection())
                            {
                                dbconn.Open();
                                using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM results WHERE @var1 <= avgval AND avgval < @var2", dbconn))
                                {
                                    cmd.Parameters.AddWithValue("@var1", MinValue);
                                    cmd.Parameters.AddWithValue("@var2", MaxValue);
                                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                                    {

                                        while (reader.Read())
                                        {
                                            values.Add(new ResultsValues(reader.GetFieldValue<int>(0), reader.GetFieldValue<DateTime>(1), reader.GetFieldValue<float>(2), reader.GetFieldValue<float>(3), reader.GetFieldValue<float>(4), reader.GetFieldValue<float>(5), reader.GetFieldValue<float>(6), reader.GetFieldValue<string>(7)));
                                        }
                                    }
                                }
                            }

                            return new ResultTuple(values, "success");
                        }
                        else
                        {
                            return new ResultTuple(null, "Values set incorrectly");
                        }
                    case "avgextime":
                        if (float.TryParse(start, out float MinExTime) &&
                            float.TryParse(end, out float MaxExTime))
                        {
                            using (NpgsqlConnection dbconn = conn.CreateConnection())
                            {
                                dbconn.Open();
                                using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM results WHERE @var1 <= avgextime AND avgextime < @var2", dbconn))
                                {
                                    cmd.Parameters.AddWithValue("@var1", MinExTime);
                                    cmd.Parameters.AddWithValue("@var2", MaxExTime);
                                    using (NpgsqlDataReader reader = cmd.ExecuteReader())
                                    {

                                        while (reader.Read())
                                        {
                                            values.Add(new ResultsValues(reader.GetFieldValue<int>(0), reader.GetFieldValue<DateTime>(1), reader.GetFieldValue<float>(2), reader.GetFieldValue<float>(3), reader.GetFieldValue<float>(4), reader.GetFieldValue<float>(5), reader.GetFieldValue<float>(6), reader.GetFieldValue<string>(7)));
                                        }
                                    }
                                }
                            }

                            return new ResultTuple(values, "success");
                        }
                        else
                        {
                            return new ResultTuple(null, "Execution Times set incorrectly");
                        }
                    default:
                        return new ResultTuple(null, "Filter name is not valid");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new ResultTuple(null, "Error encountered");
            }            
        }

        [HttpGet("ShowValues",Name = "ShowLastTenValues")]
        public ValuesTuple ShowValues(string filename)
        {
            try
            {
                List<float> values = new List<float>();
                using (NpgsqlConnection dbconn = conn.CreateConnection())
                {
                    dbconn.Open();
                    using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT sub.value FROM " +
                                                                "(SELECT value, date FROM values WHERE filename = @var1 ORDER BY date DESC LIMIT 10)" +
                                                                "sub ORDER BY date ASC", dbconn))
                    {
                        cmd.Parameters.AddWithValue("@var1", filename);
                        using (NpgsqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                values.Add(reader.GetFieldValue<float>(0));
                            }
                        }
                    }
                }
                return new ValuesTuple(values, "success");
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new ValuesTuple(null, "Error encountered");
            }

        }
    }
}
