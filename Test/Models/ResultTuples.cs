namespace Test.Models
{
    public class ResultTuple
    {
        public List<ResultsValues>? Values { get; set; }
        public string Message { get; set; }
        public ResultTuple(List<ResultsValues> values, string message)
        {
            Values = values;
            Message = message;
        }
    }

    public class ValuesTuple
    {
        public List<float>? Values { get; set; }
        public string Message { get; set; }
        public ValuesTuple(List<float>? values, string message) 
        { 
            Values = values; 
            Message = message; 
        }
    }
}
