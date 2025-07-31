using System.Text.RegularExpressions;
using Test.Models;

namespace Test.Modules
{
    public class FirstCallFunc
    {
        private DateTime MinDate = DateTime.Now;
        private DateTime MaxDate = new DateTime(2000, 1, 1);
        private float AvgExTime = 0;
        private float AvgVal = 0;
        private float MinVal = 0;
        private float MaxVal = 0;
        private string FileName = "";
        public void ReadCSVStream(IFormFile file, out List<string> result)
        {
            result = new List<string>();
            using (StreamReader sr = new StreamReader(file.OpenReadStream()))
            {
                while (!sr.EndOfStream)
                {
                    result.Add(sr.ReadLine());
                }
            }
        }

        public string ParseCSV(List<string> TempLines, string filename, out List<CSVValues> DBentries)
        {
            DBentries = new List<CSVValues>();
            List<string> TempLineVal;
            DateTime Date;
            int ExecutionTime;
            float Value;
            FileName = filename;

            for (int i = 0; i < TempLines.Count; i++)
            {
                TempLineVal = TempLines[i].Split(";", StringSplitOptions.None).ToList();
                if (TempLineVal.Count > 3)
                {
                    TempLineVal.RemoveAt(3);
                }
                TempLineVal[0] = Regex.Replace(TempLineVal[0], @"\d\d-\d\d-\d\d\.", m => m.Value.Replace('-', ':'));

                //Parsing
                if (TempLineVal.Any(Val => Val.Length == 0))
                {
                    //Abort
                    return "File does not contain data on row " + (i + 1).ToString();
                }
                else
                {
                    if (DateTime.TryParse(TempLineVal[0], out Date) &&
                        int.TryParse(TempLineVal[1], out ExecutionTime) &&
                        float.TryParse(TempLineVal[2], out Value))
                    {
                        //Checks
                        if (Date.CompareTo(DateTime.Now) > 0 ||
                            Date.CompareTo(new DateTime(2000, 1, 1)) < 0 ||
                            ExecutionTime < 0 ||
                            Value < 0)
                        {
                            //Abort
                            return "Data in not up to set standart in row " + (i + 1);
                        }

                        //Save to list
                        DBentries.Add(new CSVValues(Date, ExecutionTime, Value, FileName));

                        //For Results
                        if (MinDate.CompareTo(Date) > 0)
                        {
                            MinDate = Date;
                        }
                        if (MaxDate.CompareTo(Date) < 0)
                        {
                            MaxDate = Date;
                        }
                        AvgExTime += ExecutionTime;
                        AvgVal += Value;
                        if (i == 0)
                        {
                            MinVal = DBentries[0].Value;
                            MaxVal = DBentries[0].Value;
                        }
                        else
                        {
                            if (MinVal > Value)
                            {
                                MinVal = Value;
                            }
                            if (MaxVal < Value)
                            {
                                MaxVal = Value;
                            }
                        }

                    }
                    else
                    {
                        //Abort
                        return "Cannot correctly parse data from row " + (i + 1) + ". Please make sure it is defined correctly";
                    }
                }
            }
            return "0";
        }

        public ResultsValues CalculateResultsExptMedVal(int count)
        {
            int ddate = (int)MaxDate.Subtract(MinDate).TotalSeconds;
            AvgExTime = AvgExTime / count;
            AvgVal = AvgVal / count;

            return new ResultsValues(ddate, MinDate, AvgExTime, AvgVal, 0, MaxVal, MinVal, FileName);
        }
    }
}
