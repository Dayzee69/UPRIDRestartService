using Oracle.ManagedDataAccess.Client;
using System;

namespace UPRIDRestartService
{
    class OracleConnect
    {
        public OracleConnection oracleConnection;
        public string rowCount;
        public string timeOut;
        public string serviceName;
        public string processName;

        public OracleConnect(string oraclehost, string oracleport, string oracledatabase, string oracleuser, string oraclepass, string rowcount,
            string timeout, string servicename, string processname)
        {
            try
            {
                oracleConnection = new OracleConnection("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=" + oraclehost + ")(PORT=" + oracleport +
                "))(CONNECT_DATA=(SERVICE_NAME=" + oracledatabase + ")));User Id=" + oracleuser + ";Password=" + oraclepass + ";");
                rowCount = rowcount;
                timeOut = timeout;
                serviceName = servicename;
                processName = processname;
            }
            catch (Exception ex)
            {
                Logger.Log.Error("SQLCONNECT ERROR " + ex.ToString());
            }

        }
    }
}
