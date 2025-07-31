namespace Test.Models
{
    public class CSVValues
    {
        public DateTime Date { get; set; }

        public int ExecutionTime { get; set; }

        public float Value { get; set; }

        public string FileName { get; set; }
        
        public CSVValues(DateTime date, int executiontime, float value, string filename)
        {
            Date = date;
            ExecutionTime = executiontime;
            Value = value;
            FileName = filename;
        }
    }
}
