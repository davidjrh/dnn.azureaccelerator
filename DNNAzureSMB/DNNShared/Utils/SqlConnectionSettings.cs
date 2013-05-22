namespace DNNShared.Utils
{
    public class SqlConnectionSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseOwner { get; set; }
        public string ObjectQualifier { get; set; }

        public SqlConnectionSettings()
        {
            DatabaseOwner = "dbo";
        }
    }
}
