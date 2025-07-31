namespace Test.Models
{
    public class ResultsValues
    {
        public int ddate {  get; set; }
        public DateTime mindate { get; set; }
        public float avgextime { get; set; }
        public float avgval { get; set; }
        public float medval { get; set; }
        public float maxval { get; set; }
        public float minval { get; set; }
        public string filename { get; set; }
        public ResultsValues(int ddate, DateTime mindate, float avgexectime, float avgval, float medval, float maxval, float minval, string filename)
        {
            this.ddate = ddate;
            this.mindate = mindate;
            this.avgextime = avgexectime;
            this.avgval = avgval;
            this.medval = medval;
            this.maxval = maxval;
            this.minval = minval;
            this.filename = filename;
        }
    }
}
