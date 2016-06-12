//     ScriptSqlConfig - Generate a script of common SQL Server configration options and objects
//
//     Copyright (c) 2011 scaleSQL Consulting, LLC
//
//    Definitions
//    ===========
//    The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same 
//        meaning here as under U.S. copyright law. 
//    A "contribution" is the original software, or any additions or changes to the software. 
//    A "contributor" is any person that distributes its contribution under this license. 
//    "Licensed patents" are a contributor's patent claims that read directly on its contribution.
//
//    Grant of Rights
//    ===============
//    (A) Copyright Grant- Subject to the terms of this license, including the license conditions and 
//        limitations in section 3, each contributor grants you a non-exclusive, worldwide, royalty-free 
//        copyright license to reproduce its contribution, prepare derivative works of its contribution, 
//        and distribute its contribution or any derivative works that you create.
//    (B) Patent Grant- Subject to the terms of this license, including the license conditions and 
//        limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
//        royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, 
//        import, and/or otherwise dispose of its contribution in the software or derivative works of 
//        the contribution in the software.
//
//    Conditions and Limitations
//    ==========================
//    (A) No Trademark License- This license does not grant you rights to use any contributors' 
//        name, logo, or trademarks. 
//    (B) If you bring a patent claim against any contributor over patents that you claim are 
//        infringed by the software, your patent license from such contributor to the software ends automatically. 
//    (C) If you distribute any portion of the software, you must retain all copyright, patent, 
//        trademark, and attribution notices that are present in the software. 
//    (D) If you distribute any portion of the software in source code form, you may do so 
//        only under this license by including a complete copy of this license with your distribution. 
//        If you distribute any portion of the software in compiled or object code form, you may only 
//        do so under a license that complies with this license. 
//    (E) The software is licensed "as-is." You bear the risk of using it. The contributors 
//        give no express warranties, guarantees, or conditions. You may have additional consumer 
//        rights under your local laws which this license cannot change. To the extent permitted 
//        under your local laws, the contributors exclude the implied warranties of merchantability, 
//        fitness for a particular purpose and non-infringement.

using System;
using System.Collections.Generic;
using System.Text;

using System.Data.Sql;
using System.Data.SqlClient;
using Microsoft.SqlServer.Management.Smo;
using Microsoft.SqlServer.Management.Smo.Agent;
using Microsoft.SqlServer.Management.Smo.Mail;
using Microsoft.SqlServer.Management.Common;
using System.Collections.Specialized;
using System.IO;

using System.Text.RegularExpressions;
using NDesk.Options;

using System.Reflection;
using ScriptSqlConfig;

namespace ScriptSqlConfig
{
	class Program
	{
		static bool SCRIPT_INSTANCE = true;
		static bool SCRIPT_DATABASES = false;
		static bool VERBOSE = false;
		static string SERVER = "";
		static string DIRECTORY = "";
		static string DATABASE = "";
		static bool SHOW_HELP = false;
		static string USER_NAME = "";
		static string PASSWORD = "";
        static bool TEST_SMO = false;


        static System.Version SERVER_VERSION = new Version();
        static System.Version SMO_VERSION = new Version();

		static void Main(string[] args)
		{
			Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;

			// use tfpt from the TFS Power Tools
			// that includes a tfpt that adds all files not in TFS.

			var p = new OptionSet () {
			{ "v|verbose",  z => VERBOSE = true },
			{ "s|server=",   z =>  SERVER = z  },
			{ "db|databases",      z => { SCRIPT_DATABASES = true; } },
			// { "nodb",      z => { SCRIPT_DATABASES = false; } },
			{ "noinstance",      z => { SCRIPT_INSTANCE = false; } },
			{ "d|dir|directory=", z =>  DIRECTORY = z } ,
			{ "scriptdb=",   z => DATABASE = z } ,
			{ "u|user=", z => USER_NAME = z },
			{ "p|password=", z => PASSWORD = z },
            { "testsmo", z => TEST_SMO = true },
			{ "h|?|help",   z => { SHOW_HELP = true; } } 
						};

			List<string> extra = p.Parse (args);

			// write a message if we find the /nodb flag
			if (extra.Contains("/nodb") || extra.Contains("/NODB"))
			{
				Console.WriteLine("");
				Console.WriteLine("*****************************************************");
				Console.WriteLine("*** WARNING: The /nodb option is no longer supported.");
				Console.WriteLine("***          Databases aren't scripted by default.");
				Console.WriteLine("*****************************************************");
				Console.WriteLine("");
			}

            WriteMessage("Launching (" + v.Major.ToString() + "." + v.Minor.ToString() + ")....");
            if (TEST_SMO)
            {
                Helper.TestSMO();
                // return;
            }

			// the server and directory are required.  No args also brings out the help
			if (SERVER.Length == 0 || DIRECTORY.Length == 0 || args.Length == 0)
				SHOW_HELP = true;

			// if they enter a username, require a password.
			if (USER_NAME.Length > 0 && PASSWORD.Length == 0)
				SHOW_HELP = true;

			
		   #region Show Help
		   if ( SHOW_HELP )
			{
				Console.WriteLine(@"
ScriptSqlConfig.EXE (" + v.ToString() + @")

	This application generates scripts and configuration information
	for many SQL Server options and database objects.

	Required Parameters:
	----------------------------------------------------------------

	/server ServerName
	/dir    OutputDirectory

	Optional Parameters:
	----------------------------------------------------------------
	
	/v          (Verbose Output)
	/databases  (Script databases)
	/noinstance (Don't script instance information)
	/user       (SQL Server user name.  It will use trusted
				 security unless this option is specified.)
	/password   (SQL Server password.  If /user is specified then
				 /password is required.)
	/scriptdb   (Database to script.  This will script a single 
				 database in addition to the instance scripts.)
	/?          (Display this help)
    /testsmo    (Test loading the SMO libraries)

	Sample Usage
	----------------------------------------------------------------

	ScriptSqlConfig.EXE /server Srv1\Instance /dir 'C:\MyDir'

	Notes
	----------------------------------------------------------------
	1. If you have spaces in the path name, enclose it in quotes.
	2. It will use trusted authentication unless both the /username
	   and /password are specified.
");

#if DEBUG
				Console.WriteLine("");
				Console.WriteLine("Press any key to continue....");
				Console.ReadLine();
#endif


				return;
			}
		   #endregion 


			
			WriteMessage("Directory: " + DIRECTORY);
			WriteMessage("Server: " + SERVER);

            /* Display the SMO version */
            var ver = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.SqlServer.Management.Smo.Server)).GetName().Version;
            WriteMessage("SMO Version: " + ver.ToString());

            /* Set the SQL Server version */
            SetVersions(SERVER);


			if (SCRIPT_INSTANCE)
				ScriptInstance(SERVER, DIRECTORY);

			if (SCRIPT_DATABASES)
				ScriptAllDatabases(SERVER, DIRECTORY);
			WriteMessage("Done.");

#if DEBUG
				Console.WriteLine("Press any key to continue....");
				Console.ReadLine();
#endif
				
		}

        private static void SetVersions(string server)
        {
            SqlConnection conn = GetConnection(server, "master");
            Server srv = new Server(new ServerConnection(conn));
            SERVER_VERSION = srv.Version;
            conn.Close();

            Assembly a1 = System.Reflection.Assembly.GetAssembly(typeof(Microsoft.SqlServer.Management.Smo.Server));
            SMO_VERSION = a1.GetName().Version;
        }

		private static void ScriptInstance(string server, string directory)
		{
			WriteMessage("Scripting Instance information...");
			SqlConnection conn = GetConnection(server, "master");
			Server srv = new Server(new ServerConnection(conn));
			
			//string instanceDirectory = Path.Combine(directory, "Instance");
			string instanceDirectory = directory;
			//DirectoryInfo d = new DirectoryInfo(instanceDirectory);
			//if (d.Exists)
			//    d.Delete(true);

			ScriptingOptions so = new ScriptingOptions();
			so.ScriptDrops = false;
			so.IncludeIfNotExists = true;
			so.ClusteredIndexes = true;
			so.DriAll = true;
			so.Indexes = true;
			so.SchemaQualify = true;
			so.Permissions = true;
			so.IncludeDatabaseRoleMemberships = true;
			so.AgentNotify = true;
			so.AgentAlertJob = true;
			
			
			if (VERBOSE)
				WriteMessage("Getting target server version through SMO");

            //try
            //{
            //    so.TargetServerVersion = GetTargetServerVersion(srv);
            //}
            //catch (ConnectionFailureException ex)
            //{
            //    WriteMessage("Error: " + ex.Message);
            //    if (ex.InnerException is SqlException)
            //    {
            //        WriteMessage("Error: " + ex.InnerException.Message);
            //    }
            //    System.Environment.Exit(1);
            //}
			so.Triggers = false;
			so.AnsiPadding = false;

			WriteServerProperties(srv, instanceDirectory);
			ScriptLogins(conn, instanceDirectory, so);
			ScriptDatabaseMail(srv, instanceDirectory, so); // There's a bug that it doesn't script the SMTP server and port.
			ScriptAgentInformation(srv, instanceDirectory, so);
			// ScriptAgentProxies(srv, instanceDirectory, so);
			ScriptLinkedServers(srv, instanceDirectory, so);
			ScriptServerAudits(srv, instanceDirectory, so);
			ScriptCredentials(srv, instanceDirectory, so);
			ScriptEventNotifications(conn, instanceDirectory, so);
			ScriptOtherObjects(srv, instanceDirectory, so);
            ScriptDatabaseOptions(srv, instanceDirectory, so);

			WriteMessage("Scripting User Objects and Security in System Databases...");
			ScriptDatabase(srv.Name.ToString(), "master", Path.Combine(instanceDirectory, @"Databases\master"));
			ScriptDatabase(srv.Name.ToString(), "msdb", Path.Combine(instanceDirectory, @"Databases\msdb"));
			ScriptDatabase(srv.Name.ToString(), "model", Path.Combine(instanceDirectory, @"Databases\model"));

//            #if DEBUG
//            ScriptDatabase(srv.Name.ToString(), "AdventureWorks", Path.Combine(instanceDirectory, @"Databases\AdventureWorks"));
//#endif
			// if we passed in a database, then script that one too.
			if (DATABASE.Length > 0)
			{
				ScriptDatabase(srv.Name.ToString(), DATABASE, Path.Combine(Path.Combine(instanceDirectory, @"Databases"), DATABASE));
			}

 
			if (conn.State == System.Data.ConnectionState.Open)
				conn.Close();
		}

		private static void ScriptEventNotifications(SqlConnection connection, string directory, ScriptingOptions so)
		{
			WriteMessage("Scripting Event Notifications...");
			StringCollection script = new StringCollection();

			script.Add(String.Format(@"--===============================================================================================
-- SERVER: {0}
--===============================================================================================

", connection.DataSource.ToString()));

			if (connection.State == System.Data.ConnectionState.Closed)
				connection.Open();
			SqlCommand cmd = new SqlCommand(@"select se.*, sen.* 
from sys.server_events se
join sys.server_event_notifications sen on sen.object_id = se.object_id
order by type_desc;", connection);

			using (SqlDataReader rdr = cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection))
			{
				while (rdr.Read())
				{
					script.Add(String.Format(@"CREATE EVENT NOTIFICATION {0}
ON {1} WITH FAN_IN 
FOR {2} 
TO SERVICE '{3}', '{4}'
GO

", rdr.GetString(rdr.GetOrdinal("name")),
	rdr.GetString(rdr.GetOrdinal("parent_class_desc")),
	rdr.GetString(rdr.GetOrdinal("type_desc")),
	rdr.GetString(rdr.GetOrdinal("service_name")),
	rdr.GetString(rdr.GetOrdinal("broker_instance"))
			));
				}
			}
			
			WriteFile(script, Path.Combine(directory, "Event Notifications.sql"), false);
		}

        //private static SqlServerVersion GetTargetServerVersion(Server srv)
        //{
        //    string version = srv.VersionMajor.ToString() + srv.VersionMinor.ToString();
        //    if (version == "80")
        //        return SqlServerVersion.Version80;
   
        //    if (version == "90")
        //        return SqlServerVersion.Version90;
   
        //    if (version == "100")
        //        return SqlServerVersion.Version100;

        //    if (version == "1050")
        //        return SqlServerVersion.Version105;

        //    if (version == "110")
        //        return SqlServerVersion.Version110;

        //    throw new Exception("Unsupported Server Version");
			
        //}

		private static void WriteServerProperties(Server smoServer, string directory)
		{

			if (VERBOSE)
				WriteMessage("Getting Configuration.Properties");


			StringCollection settings = new StringCollection();
			try
			{
				foreach (ConfigProperty p in smoServer.Configuration.Properties)
				{
					//if (VERBOSE)
					//    WriteMessage(p.DisplayName);
					try
					{
						settings.Add(p.DisplayName + " [" + p.RunValue.ToString() + "]");
					}
					catch
					{
						settings.Add(p.DisplayName + " - Failed to query property. (SMO Version conflict.)");
					}

				}
				WriteFile(settings, Path.Combine(directory, "sp_configure.txt"));
			}
			catch 
			{
				WriteMessage("Error: The infamous sp_ProcessorUsage bug prevents writing properties.");
			}

			if (VERBOSE)
				WriteMessage("Getting smoServer.Properties");

			settings.Clear();


			Microsoft.SqlServer.Management.Smo.Property x;
			for (int i = 0; i <= smoServer.Properties.Count; i++)
			{
				try
				{
					x = smoServer.Properties[i];
					object propertyValue = x.Value ?? (object)"NULL";
					if (x.Name != "PhysicalMemoryUsageInKB") // this value varies too much each time
					{
						settings.Add(x.Name + (" [" + propertyValue.ToString() + "]") ?? "");
					}
				}
				catch (Exception)
				{ 
				//nothing to do, just catch the exception
				}
			}
			WriteFile(settings, Path.Combine(directory, "Properties.txt"));


			//try
			//{
			//    foreach (Microsoft.SqlServer.Management.Smo.Property x in smoServer.Properties)
			//    {
			//        try
			//        {
			//            object propertyValue = x.Value ?? (object)"NULL";
			//            settings.Add(x.Name + (" [" + propertyValue.ToString() + "]") ?? "");
			//        }
			//        catch
			//        {
			//            settings.Add(x.Name + (" *** Error getting property value ***") ?? "");
			//        }
			//    }
			//    WriteFile(settings, Path.Combine(directory, "Properties.txt"));
			//}
			//catch
			//{
			//    WriteMessage("Error: The infamous sp_ProcessorUsage bug prevents writing properties.");
			//}

		}
		private static void ScriptLogins(SqlConnection connection, string directory, ScriptingOptions options)
		{



			WriteMessage("Scripting Logins...");
			StringCollection script = new StringCollection();

			StringCollection defaultDatabaseScript = new StringCollection();

			#region Logins
			script.Add(String.Format(@"--===============================================================================================
-- SERVER: {0}
--===============================================================================================

", connection.DataSource.ToString()));

			if (connection.State == System.Data.ConnectionState.Closed)
				connection.Open();
			SqlCommand cmd = new SqlCommand(@"SELECT P.[name], P.[sid] , L.[password_hash], P.[default_database_name], L.[is_expiration_checked], L.[is_policy_checked], P.[type_desc], P.[is_disabled]
FROM [sys].[server_principals] P
LEFT JOIN [sys].[sql_logins] L ON L.[principal_id] = P.[principal_id]
where P.[type_desc] In ('WINDOWS_GROUP', 'WINDOWS_LOGIN', 'SQL_LOGIN' )
AND P.[name] not like 'BUILTIN%'
and P.[NAME] not like 'NT AUTHORITY%'
and P.[name] not like '%\SQLServer%'
and P.[name] not like 'NT Service%'
and P.[name] not in ('sa', 'guest')
and P.[name] not like '##%'
ORDER BY P.[name]", connection);


			string createLogin;
			using (SqlDataReader rdr = cmd.ExecuteReader())
			{
				while (rdr.Read())
				{

					string type_desc = rdr.GetString(rdr.GetOrdinal("type_desc"));
					string default_database = rdr.GetString(rdr.GetOrdinal("default_database_name"));
					string login_name = rdr.GetString(rdr.GetOrdinal("name"));

                    string table_to_check_for_existance = ""; // this is either sys_logins or server_principals

					script.Add(String.Format(@"
---------------------------------------------------------------------------------------------
-- Login: {0} 
---------------------------------------------------------------------------------------------
", login_name));
					switch (type_desc)
					{
						case "WINDOWS_LOGIN":
						case "WINDOWS_GROUP":
                            table_to_check_for_existance = "server_principals";

							createLogin = @"IF NOT EXISTS (SELECT * FROM [master].[sys].[server_principals] WHERE [name] = '" + rdr.GetString(rdr.GetOrdinal("name")) + @"')
	CREATE LOGIN [" + rdr.GetString(rdr.GetOrdinal("name")) + @"] FROM WINDOWS;
GO

";
							script.Add(createLogin);

							if (default_database.ToLower() != "master")
							{
								string alterDatabase = string.Format(@"IF EXISTS (SELECT * FROM [master].[sys].[server_principals] WHERE [name] = '{0}')
    AND EXISTS (SELECT * FROM [master].[sys].[databases] WHERE [name] = '{1}')
	    ALTER LOGIN [{0}] WITH DEFAULT_DATABASE=[{1}];
GO

", login_name, default_database);

								script.Add(alterDatabase);
								defaultDatabaseScript.Add(alterDatabase);
							}
						break;

						case "SQL_LOGIN":

                        #region SQL_LOGIN

                        table_to_check_for_existance = "sql_logins";
                        byte[] rawSid = new byte[85];
						long length = rdr.GetBytes(rdr.GetOrdinal("sid"), 0, rawSid, 0, 85);
						string Sid = "0x" + BitConverter.ToString(rawSid).Replace("-", String.Empty).Substring(0, (int)length * 2);

						byte[] rawPasswordHash = new byte[256];
						length = rdr.GetBytes(rdr.GetOrdinal("password_hash"), 0, rawPasswordHash, 0, 256);
						string passwordHash = "0x" + BitConverter.ToString(rawPasswordHash).Replace("-", String.Empty).Substring(0, (int)length * 2);

						createLogin = @"IF NOT EXISTS (SELECT * FROM [master].[sys].[sql_logins] WHERE [name] = '" + rdr.GetString(rdr.GetOrdinal("name")) + @"')
	CREATE LOGIN [" + rdr.GetString(rdr.GetOrdinal("name")) + @"] 
		WITH PASSWORD = " + passwordHash + @" HASHED,
		SID = " + Sid + @",  CHECK_POLICY=OFF, ";

						if (rdr.GetBoolean(rdr.GetOrdinal("is_expiration_checked")))
							createLogin += "CHECK_EXPIRATION = ON";
						else
							createLogin += "CHECK_EXPIRATION = OFF";


						createLogin += @"
GO

";

						if (default_database.ToLower() != "master")
						{
							string alterDatabase = string.Format(@"IF EXISTS (SELECT * FROM [master].[sys].[sql_logins] WHERE [name] = '{0}')
    AND EXISTS (SELECT * FROM [master].[sys].[databases] WHERE [name] = '{1}')
        ALTER LOGIN [{0}] WITH DEFAULT_DATABASE=[{1}];
GO

", login_name, default_database);

							createLogin += alterDatabase;
							defaultDatabaseScript.Add(alterDatabase);
						}

						createLogin += @"IF EXISTS (SELECT * FROM [master].[sys].[sql_logins] WHERE [name] = '" + rdr.GetString(rdr.GetOrdinal("name")) + @"')
    ALTER LOGIN [" + rdr.GetString(rdr.GetOrdinal("name")) + @"] WITH ";
						if (rdr.GetBoolean(rdr.GetOrdinal("is_expiration_checked")))
							createLogin += "CHECK_EXPIRATION = ON";
						else
							createLogin += "CHECK_EXPIRATION = OFF";

						createLogin += ", ";

						if (rdr.GetBoolean(rdr.GetOrdinal("is_policy_checked")))
							createLogin += "CHECK_POLICY = ON";
						else
							createLogin += "CHECK_POLICY = OFF";


						createLogin += @"
GO

";

						script.Add(createLogin);
#endregion 
							break; // SQL Login

							default:
							WriteMessage(String.Format("Unknown Loging Type [{0}] for [{1}]", type_desc, login_name));
							break;

					}

					if (rdr.GetBoolean(rdr.GetOrdinal("is_disabled")) == true)
					{
					script.Add(string.Format(@"IF EXISTS (SELECT * FROM [master].[sys].[{0}] WHERE [name] = '{1}')
	ALTER LOGIN [{1}] DISABLE
GO

", table_to_check_for_existance, login_name));
					}

					#region Group Membership

					SqlConnection groupConn = new SqlConnection(connection.ConnectionString);

					SqlCommand groupCmd = new SqlCommand(@"select l.[name] AS LoginName, r.name AS RoleName
from [master].[sys].[server_role_members] rm
join [master].[sys].[server_principals] r on r.[principal_id] = rm.[role_principal_id]
join [master].[sys].[server_principals] l on l.[principal_id] = rm.[member_principal_id]
where l.[name] = @login_name
/* AND l.[name] not in ('sa')
AND l.[name] not like 'BUILTIN%'
and l.[NAME] not like 'NT AUTHORITY%'
and l.[name] not like '%\SQLServer%'
and l.[name] not like '##%'
and l.[name] not like 'NT Service%' */
ORDER BY r.[name], l.[name]", groupConn);

					groupCmd.Parameters.Add("login_name", System.Data.SqlDbType.NVarChar, 128).Value = login_name;

					groupConn.Open();
					using (SqlDataReader groupReader = groupCmd.ExecuteReader())
					{
						if (groupReader.HasRows)
						{

							while (groupReader.Read())
							{
								script.Add(String.Format(@"IF EXISTS (SELECT * FROM [master].[sys].[{2}] WHERE [name] = '{0}')
	EXEC sp_addsrvrolemember @loginame = N'{0}', @rolename = N'{1}'
GO

", login_name, groupReader.GetString(groupReader.GetOrdinal("RoleName")), table_to_check_for_existance));
							}
						}
						groupReader.Close();
						groupConn.Close();
					}
					#endregion

					

				}
				rdr.Close();
			}
#endregion

			#region Default Databases

			// add the default databases a second time so it can be run separately
			script.Add(@"-------------------------------------------------------------------------------------
-- Default databases are repeated so you can rerun them after mirrors are failed over 
-------------------------------------------------------------------------------------");

			if (defaultDatabaseScript.Count > 0)
			{
				script.Append(defaultDatabaseScript);
            }
            #endregion


            WriteFile(script, Path.Combine(directory, "Logins.sql"));
		}

		private static void ScriptDatabaseMail(Server smoServer, string directory, ScriptingOptions options)
		{
			// if this is express, then don't script database mail
			if (smoServer.EngineEdition == Edition.Express)
				return; 


			WriteMessage("Scripting Database Mail...");
			StringCollection script = new StringCollection();
			
			// Script the configuration values
			foreach (ConfigurationValue cv in smoServer.Mail.ConfigurationValues)
			{
				script.Append(cv.Script(options));
			}
			script.Add("GO" + Environment.NewLine);

			// Script the accounts
			foreach (MailAccount ma in smoServer.Mail.Accounts)
			{
				script.Append(ma.Script(options));
				script.Add("GO" + Environment.NewLine);

				// script the SMTP server for each account
				foreach (MailServer ms in ma.MailServers)
				{
					script.Append(ms.Script(options));
					script.Add("GO" + Environment.NewLine);
				}
			}

			// Script each profile
			foreach (MailProfile mp in smoServer.Mail.Profiles)
			{
				script.Append(mp.Script(options));
				script.Add("END");  // SMO doesn't script the END statement
				script.Add("GO" + Environment.NewLine);
			}

			WriteFile(script, Path.Combine(directory, "DatabaseMail.sql"));
		}

		private static void ScriptIndividualJobs(Server smoServer, string directory, ScriptingOptions options)
		{
			
			
			// delete any remaining job files
			
			string workingDirectory = Path.Combine(directory, "SQL Server Agent");
			workingDirectory = Path.Combine(workingDirectory, "SQL Server Agent Jobs");
			
			// Get a list of files in the directory
			StringCollection files = new StringCollection();
			if (Directory.Exists(workingDirectory))
				files.AddRange(Directory.GetFiles(workingDirectory));

			// Script each job, save it, and delete the entry for the job
			foreach (Job j in smoServer.JobServer.Jobs)
			{
				string jobName = CleanUpFileName(j.Name);
				StringCollection script = j.Script(options);
				string fileName = Path.Combine(workingDirectory, jobName + ".sql");
				WriteFile(script, fileName);
				
				// remove this file name from our list of files
				files.Remove(fileName);
			}

			// Remove any files that are left
			foreach (string extraFile in files)
			{
				File.Delete(extraFile);
			}


		}

        private static void MoveFile(string fileName, string sourcePath, string destinationPath)
        {
            if (!File.Exists(Path.Combine(sourcePath, fileName)))
                return;

            if (File.Exists(Path.Combine(destinationPath, fileName)))
                return;

            File.Move(Path.Combine(sourcePath, fileName), Path.Combine(destinationPath, fileName));
        }
		private static void ScriptAgentInformation(Server smoServer, string directory, ScriptingOptions options)
		{
			if (smoServer.EngineEdition != Edition.Express)
			{

				//TODO: Add job categories

				WriteMessage("Scripting Agent Information...");

                // Move any existing files to their new location (added in 2012.4)
                // Previously they were scripted in the main server directory)
                string agentDirectory = Path.Combine(directory, "SQL Server Agent");

                // Create the directory
                if (!Directory.Exists(agentDirectory))
                    Directory.CreateDirectory(agentDirectory);


                MoveFile("SQL Server Agent Jobs.sql", directory, agentDirectory);
                MoveFile("SQL Server Agent Operators.sql", directory, agentDirectory);
                MoveFile("SQL Server Agent Alerts.sql", directory, agentDirectory);
                MoveFile("SQL Server Agent Properties.txt", directory, agentDirectory);
                MoveFile("SQL Server Agent Proxy Accounts.sql", directory, agentDirectory);

				StringCollection script = smoServer.JobServer.Jobs.Script(options);
				WriteFile(script, Path.Combine(agentDirectory,"SQL Server Agent Jobs.sql"), true);

				ScriptIndividualJobs(smoServer, directory, options);

				script = smoServer.JobServer.Operators.Script(options);
                WriteFile(script, Path.Combine(agentDirectory, "SQL Server Agent Operators.sql"));

				script = smoServer.JobServer.AlertSystem.Script(options);
				script.Append(smoServer.JobServer.Alerts.Script(options));
				script.Insert(0, @"------------------------------------------------------------------
-- NOTE: Alert notifications aren't scripted in this release.
------------------------------------------------------------------

");

                WriteFile(script, Path.Combine(agentDirectory, "SQL Server Agent Alerts.sql"));

				script = new StringCollection();
				SqlPropertyCollection p = smoServer.JobServer.Properties;
				foreach (Microsoft.SqlServer.Management.Smo.Property i in p)
				{
					//Console.WriteLine("Name: {0}", i.Name.ToString());
					script.Add(i.Name.ToString() + " [" + i.Value.ToString() + "]");
				}

                WriteFile(script, Path.Combine(agentDirectory, "SQL Server Agent Properties.txt"));

				// Script Proxies
				script = new StringCollection();
				foreach (ProxyAccount a in smoServer.JobServer.ProxyAccounts)
				{
					script.Append(a.Script(options));
				}
                WriteFile(script, Path.Combine(agentDirectory, "SQL Server Agent Proxy Accounts.sql"));

				//script = smoServer.Mail.Script();
			}
			
		}

		//private static void ScriptAgentProxies(Server smoServer, string directory, ScriptingOptions options)
		//{
		//    if (smoServer.EngineEdition != Edition.Express)
		//    {
		//        WriteMessage("Scripting Agent Proxies...");
		//        StringCollection script = new StringCollection();
		//        string proxyDirectory = Path.Combine(directory, "SQL Server Agent");
		//        //proxyDirectory = Path.Combine(proxyDirectory, "Proxies");

		//        //RemoveSqlFiles(proxyDirectory);

		//        foreach (Microsoft.SqlServer.Management.Smo.Agent.ProxyAccount proxy in smoServer.JobServer.ProxyAccounts)
		//        {
		//            script.(proxy.Script().);
		//            WriteFile(script, Path.Combine(proxyDirectory, proxy.Name + ".sql"));
		//        }
		//        //script = smoServer.Mail.Script();
		//    }

		//}

		private static void ScriptLinkedServers(Server smoServer, string directory, ScriptingOptions options)
		{
			WriteMessage("Scripting Linked Servers...");
			StringCollection script = new StringCollection();

			//string linkedServerDirectory = Path.Combine(directory, "Linked Servers");
			//RemoveSqlFiles(linkedServerDirectory);

			foreach (LinkedServer linkedServer in smoServer.LinkedServers)
			{
				script.Append(linkedServer.Script(options));

				//string serverName = linkedServer.Name.Replace(@"\", "_");
				WriteFile(script, Path.Combine(directory,  "Linked Servers.sql"), true);
			}
		}

		private static void ScriptServerAudits(Server smoServer, string directory, ScriptingOptions options)
		{

			if (smoServer.VersionMajor >= 10)
			{
				WriteMessage("Scripting Audits...");
				StringCollection script = new StringCollection();

				foreach (Audit audit in smoServer.Audits)
				{
					script.Append(audit.Script(options));
					
				}

				foreach (ServerAuditSpecification serverAudit in smoServer.ServerAuditSpecifications)
				{
					script.Append(serverAudit.Script(options));

				}


				foreach (Audit audit in smoServer.Audits)
				{
					if (audit.Enabled)
					{
						string enableStatement = String.Format(@"ALTER SERVER AUDIT [{0}] WITH (STATE = ON);
", audit.Name);
						script.Add(enableStatement);

					}

				}


				WriteFile(script, Path.Combine(directory, "Audits.sql"), true);
			}

		}

		private static void ScriptOtherObjects(Server smoServer, string directory, ScriptingOptions options)
		{
			options.Permissions = true;

			WriteMessage("Scripting Endpoints...");
			StringCollection script = new StringCollection();
			foreach (Endpoint endPoint in smoServer.Endpoints)
			{
				if (endPoint.IsSystemObject == false)
				{
					script.Append(endPoint.Script(options));
				}
			}

			WriteFile(script, Path.Combine(directory, "Endpoints.sql"), true);

			   
		}

        private static void ScriptDatabaseOptions(Server smoServer, string directory, ScriptingOptions options)
        {
            WriteMessage("Scripting Database Options...");
            StringCollection script = new StringCollection();
            script.Add(string.Format("-- Server: {0}", smoServer.Name));
            foreach (Database db in smoServer.Databases)
            {
                if (db.IsAccessible && db.Name != "tempdb" && !db.IsDatabaseSnapshot && !db.IsSystemObject)
                {
                    // Script the database owner
                    script.Add("------------------------------------------------------------------------");
                    string sql = String.Format(@"USE [{0}]
GO
EXEC dbo.sp_changedbowner @loginame = N'{1}', @map = false
GO

", db.Name, db.Owner);

                    script.Add(sql);

                    // Script the trustworthy setting
                    if (db.Trustworthy)
                    {
                        sql = string.Format("ALTER DATABASE [{0}] SET TRUSTWORTHY ON;\r\nGO\r\n", db.Name);
                        script.Add(sql);
                    }
                    else
                    {
                        sql = string.Format("ALTER DATABASE [{0}] SET TRUSTWORTHY OFF;\r\nGO\r\n", db.Name);
                        script.Add(sql);
                    }

                }
            }

            WriteFile(script, Path.Combine(directory, "Database Options.sql"), false);
        }


		private static void ScriptCredentials(Server smoServer, string directory, ScriptingOptions options)
		{
            //smoServer.ev
			WriteMessage("Scripting Credentials...");
			StringCollection script = new StringCollection();
            script.Add(GetHeaderCommentBlock("Credentials"));
			foreach (Credential cred in smoServer.Credentials)
			{

                script.Add(String.Format(@"IF NOT EXISTS(select * from master.sys.credentials where [name] = '{0}')
    CREATE CREDENTIAL [{0}] WITH IDENTITY = N'{1}', SECRET = N'___password___'", cred.Name, cred.Identity));
			}

			WriteFile(script, Path.Combine(directory, "Credentials.sql"), true);
		}



		
		private static void ScriptAllDatabases(string server, string directory)
		{
			WriteMessage("Scripting databases...");
			SqlConnection conn = GetConnection(server, "master");
			Server srv = new Server(new ServerConnection(conn));
			string databasesDirectory = Path.Combine(directory, "Databases");



			foreach (Database db in srv.Databases)
			{
				if (db.IsAccessible && db.Name != "tempdb" && !db.IsDatabaseSnapshot)
				{
					WriteMessage("Scripting Database: " + db.Name);
					// WriteMessage(db.IsSystemObject.ToString());
					string outputDirectory = Path.Combine(databasesDirectory, db.Name);
					DirectoryInfo d = new DirectoryInfo(outputDirectory);
					//if (d.Exists)
					//    d.Delete(true);

					ScriptDatabase(server, db.Name, outputDirectory);
				}
				else
				{
					WriteMessage("Skipping Database: " + db.Name);
				}
			}
		}

		public static void WriteMessage(string message)
		{
			string output = message;
			if (VERBOSE)
				output = DateTime.Now.ToString() + " : " + output;
			Console.WriteLine(output);
		}



		private static void ScriptDatabase(string server, string database, string directory)
		{

			string[] defaultFields = new string[2] { "IsSystemObject", "IsEncrypted" };
			ScriptingOptions so = new ScriptingOptions();
			so.ScriptDrops = false;
			so.IncludeIfNotExists = true;
			so.ClusteredIndexes = true;
			so.DriAll = true;
			so.Indexes = true;
			so.SchemaQualify = true;
			
			so.Triggers = false;
			so.AnsiPadding = false;
			so.Permissions = true;
			so.IncludeDatabaseRoleMemberships = true;

			SqlConnection conn = GetConnection(server, database);
			Server srv = new Server(new ServerConnection(conn));

			// so.TargetServerVersion = GetTargetServerVersion(srv);
            // Console.WriteLine(so.TargetServerVersion);
			Database db = srv.Databases[database];

			srv.SetDefaultInitFields(typeof(StoredProcedure), "IsSystemObject");
			string objectDir;

			// Script the database properties - on hold
			//StringCollection databaseProperties = db.Script(so);
			//WriteFile(databaseProperties, Path.Combine(directory, "T1.txt"), true);
			//foreach (Property p in db.Properties)
			//{ Console.WriteLine("{0} - {1}", p.Name, p.Value); }


			#region Tables
			objectDir = Path.Combine(directory, "Tables");

			srv.SetDefaultInitFields(typeof(Table), new string[1] { "IsSystemObject" });

			StringCollection nonClusteredIndexes = new StringCollection();

			// put one use database at the top
			nonClusteredIndexes.Add(@"USE [" + database + "]" + Environment.NewLine);


			foreach (Table t in db.Tables)
			{

				if (!t.IsSystemObject)
				{
					if ( VERBOSE )
						WriteMessage("Table: " + t.Name);
					StringCollection sc = t.Script(so);

					// Script any triggers.  Encrypted triggers are listed but not scripted.
					if (t.Triggers.Count > 0)
					{
						foreach (Trigger trg in t.Triggers)
						{
							if (!trg.IsEncrypted)
							{
								StringCollection triggerScript = trg.Script(so);
								sc.Append(triggerScript);
							}
							else
							{
								string encryptedTrigger = String.Format("-- Trigger {0} is encrypted and can't be scripted.", trg.Name);
								sc.Add(encryptedTrigger);
							}
						}
					}

					// Script all non-clustered indexes to a separate file
					if (t.HasIndex)
					{
						foreach (Index idx in t.Indexes)
						{
							if (!idx.IsClustered)
							{
								nonClusteredIndexes.Append(idx.Script(so));
							}
						}
					}
					string fileName = Path.Combine(objectDir, CleanUpFileName(t.Schema) + "." + CleanUpFileName(t.Name) + ".sql");

					WriteFile(sc, fileName, true);
				}
			}

			// Write the non-clustered indexes file
			WriteFile(nonClusteredIndexes, Path.Combine(directory, "Non-Clustered Indexes.sql"), true);
			#endregion

			


			#region Stored Procedures
			objectDir = Path.Combine(directory, "Sprocs");

			srv.SetDefaultInitFields(typeof(StoredProcedure), defaultFields);
			foreach (StoredProcedure sp in db.StoredProcedures)
			{
				if (!sp.IsSystemObject && !sp.IsEncrypted)
				{
					if (VERBOSE)
						WriteMessage("Sproc: " + sp.Name);
					StringCollection sc = sp.Script(so);

					string fileName = Path.Combine(objectDir, CleanUpFileName(sp.Schema) + "." + CleanUpFileName(sp.Name) + ".sql");

					WriteFile(sc, fileName, true);
				}

			}


			#endregion

			#region User-defined data types
			objectDir = Path.Combine(directory, "DataTypes");

			
			foreach (UserDefinedDataType udt in db.UserDefinedDataTypes)
			{

				if (VERBOSE)
					WriteMessage("DataType: " + udt.Name);
				StringCollection sc = udt.Script(so);
				string fileName = Path.Combine(objectDir, CleanUpFileName(udt.Schema) + "." + CleanUpFileName(udt.Name) + ".sql");

				WriteFile(sc, fileName, true);
				
			}
			#endregion

			#region Views
			objectDir = Path.Combine(directory, "Views");

			srv.SetDefaultInitFields(typeof(View), defaultFields);
			foreach (View v in db.Views)
			{
				if (!v.IsSystemObject && !v.IsEncrypted)
				{
					if (VERBOSE)
						WriteMessage("View: " + v.Name);
					StringCollection sc = v.Script(so);
					string fileName = Path.Combine(objectDir, CleanUpFileName(v.Schema) + "." + CleanUpFileName(v.Name) + ".sql");

					WriteFile(sc, fileName, true);
				}
			}
			#endregion

			#region Triggers
			objectDir = Path.Combine(directory, "DDLTriggers");

			srv.SetDefaultInitFields(typeof(Trigger), defaultFields);
			foreach (DatabaseDdlTrigger tr in db.Triggers)
			{
				if (!tr.IsSystemObject  && !tr.IsEncrypted)
				{
					if (VERBOSE)
						WriteMessage("DDL Trigger: " + tr.Name);
					StringCollection sc = tr.Script(so);
					string fileName = Path.Combine(objectDir, CleanUpFileName(tr.Name) + ".sql");

					WriteFile(sc, fileName, true);
				}
			}
			#endregion


			#region Table Types
			if (srv.VersionMajor >= 10)
			{
				objectDir = Path.Combine(directory, "TableTypes");

				
				foreach (UserDefinedTableType tt in db.UserDefinedTableTypes)
				{

					if (VERBOSE)
						WriteMessage("TableType: " + tt.Name);
					StringCollection sc = tt.Script(so);
					string fileName = Path.Combine(objectDir, CleanUpFileName(tt.Name) + ".sql");

					WriteFile(sc, fileName, true);

				}
			}
			#endregion

			#region Assemblies
			//objectDir = Path.Combine(directory, "Assemblies");
			//RemoveSqlFiles(objectDir);
			//foreach (SqlAssembly asm in db.Assemblies)
			//{
			//    if (!asm.IsSystemObject)
			//    {
			//        WriteMessage("Assembly: " + asm.Name);
			//        StringCollection sc = asm.Script(so);
			//        string fileName = Path.Combine(objectDir,asm.Name + ".sql");

			//        WriteFile(sc, fileName);
			//    }
			//}
			#endregion

			#region User-Defined Functions
			objectDir = Path.Combine(directory, "UDF");

			srv.SetDefaultInitFields(typeof(UserDefinedFunction), defaultFields);
			foreach (UserDefinedFunction udf in db.UserDefinedFunctions)
			{
				if (!udf.IsSystemObject && !udf.IsEncrypted)
				{
					if (VERBOSE)
						WriteMessage("UDF: " + udf.Name);
					StringCollection sc = udf.Script(so);
					string fileName = Path.Combine(objectDir, CleanUpFileName(udf.Schema) + "." + CleanUpFileName(udf.Name) + ".sql");

					WriteFile(sc, fileName, true);
				}
			}
			#endregion



			#region Users
			ScriptDatabaseUsers(db, directory, so);
			ScriptDatabaseRoles(db, directory, so);
			// ScriptPermissions(db, directory, so);
			#endregion

			ScriptServiceBroker(db, directory, so);

            ScriptSchemas(db, directory, so);

		}

        private static void ScriptSchemas(Database db, string directory, ScriptingOptions so)
        {
            so.Permissions = true;

            StringCollection sc = new StringCollection();
            sc.Add(GetHeaderCommentBlock("Schemas"));
            foreach (Microsoft.SqlServer.Management.Smo.Schema schema in db.Schemas)
            {
                // SQL Server 2008 SMO is having trouble scripting schema objects - IsSystemObject is failing
                //if (SMO_VERSION.Major >= 11)
                //{
                //    if (1==1 /*!schema.IsSystemObject */)
                //    {
                //        if (VERBOSE)
                //            WriteMessage("Schema: " + schema.Name);
                //        sc.Append(schema.Script(so));
                //    }

                //}
                //else
                //{
                    if (VERBOSE)
                        WriteMessage("Schema: " + schema.Name);
                    sc.Append(schema.Script(so));
                //}

                
            }

            string fileName = Path.Combine(directory, "Schemas.sql");
            WriteFile(sc, fileName, true);
        }
		#region ScriptPermissions
		// This code is commented out until I get more time to work on it.
		//private static void ScriptPermissions(Database db, string directory, ScriptingOptions so)
		//{
		//    if (VERBOSE)
		//        WriteMessage("Permissions...");

		//    StringCollection sc = new StringCollection();
		//    DatabasePermissionInfo[] dbPerms = db.EnumDatabasePermissions();
		//    foreach (DatabasePermissionInfo i in dbPerms)
		//    {


		//        // WriteMessage(i.ToString());
		//    }

		//    System.Data.DataTable objects = db.EnumObjects();
		//    foreach (System.Data.DataRow r in objects.Rows)
		//    {
		//        WriteMessage(r["Name"].ToString());
		//    }

		//    ObjectPermissionInfo[] objPerms = db.EnumObjectPermissions();
		//    foreach (ObjectPermissionInfo o in objPerms)
		//    {
		//        //WriteMessage(o.ToString());
		//    }

		//}
		#endregion


		private static void ScriptServiceBroker(Database db, string directory, ScriptingOptions so)
		{
			so.Permissions = true;

			#region Routes
			StringCollection sc = new StringCollection();
			sc.Add(GetHeaderCommentBlock("Routes"));
			foreach (Microsoft.SqlServer.Management.Smo.Broker.ServiceRoute route in db.ServiceBroker.Routes)
			{
				if (VERBOSE)
					WriteMessage("Route: " + route.Name);

				sc.Append(route.Script(so));

			}
			#endregion

			#region Message Types
			sc.Add(GetHeaderCommentBlock("Message Types"));

			foreach (Microsoft.SqlServer.Management.Smo.Broker.MessageType messageType in db.ServiceBroker.MessageTypes)
			{
				if (!messageType.IsSystemObject)
					sc.Append(messageType.Script(so));
			}
			#endregion

			#region Queues
			sc.Add(GetHeaderCommentBlock("Queues"));
			
			foreach (Microsoft.SqlServer.Management.Smo.Broker.ServiceQueue queue in db.ServiceBroker.Queues)
			{
				if (!queue.IsSystemObject)
					sc.Append(queue.Script(so));
			}
			#endregion

			#region Contracts
			sc.Add(GetHeaderCommentBlock("Contracts"));

			foreach (Microsoft.SqlServer.Management.Smo.Broker.ServiceContract contract in db.ServiceBroker.ServiceContracts)
			{
				if (!contract.IsSystemObject)
					sc.Append(contract.Script(so));
			}
			#endregion

			#region Services
			sc.Add(GetHeaderCommentBlock("Services"));
			
			foreach (Microsoft.SqlServer.Management.Smo.Broker.BrokerService service in db.ServiceBroker.Services)
			{
				if (!service.IsSystemObject)
					sc.Append(service.Script(so));
			}
			#endregion

			string fileName = Path.Combine(directory, "Service Broker.sql");
			WriteFile(sc, fileName, true);

		}

		private static void ScriptDatabaseUsers(Database db, string directory, ScriptingOptions so)
		{
			StringCollection sc = new StringCollection();
			foreach (User u in db.Users)
			{

				if (!u.IsSystemObject)
				{
					if (VERBOSE)
						WriteMessage("User: " + u.Name);
					//u.Script(so).
					StringCollection thisScript = u.Script(so);
					sc.Append(thisScript);

				}
			}
			string fileName = Path.Combine(directory, "Users.sql");
			WriteFile(sc, fileName, true);
		}


		private static void ScriptDatabaseRoles(Database db, string directory, ScriptingOptions so)
		{
			if (VERBOSE)
				WriteMessage("Roles...");

			StringCollection sc = new StringCollection();
			foreach (DatabaseRole r in db.Roles)
			{
				if (!r.IsFixedRole)
				{
					// so.
					StringCollection thisScript = r.Script(so);
					foreach (string s in thisScript)
					sc.Append(thisScript);
				}

				
			}

			string fileName = Path.Combine(directory, "Database Roles.sql");
			WriteFile(sc, fileName, true);

			// Application roles-----------------------------------------------------------------
			sc = new StringCollection();
			foreach (ApplicationRole ar in db.ApplicationRoles)
			{
				StringCollection thisScript = ar.Script(so);
				sc.Append(thisScript);
			}

			fileName = Path.Combine(directory, "Application Roles.sql");
			WriteFile(sc, fileName, true);
			
		}

		private static void RemoveSqlFiles(string directory)
		{
			throw new NotImplementedException("RemoveSqlFiles shouldn't be called.");
			//DirectoryInfo dir = new DirectoryInfo(directory);
			//if (dir.Exists)
			//{
			//    foreach (FileInfo f in dir.GetFiles("*.sql"))
			//        f.Delete();
			//}
		}

		private static void WriteFile(StringCollection script, string fileName)
		{
			WriteFile(script, fileName, false);
		}
		private static void WriteFile(StringCollection script, string fileName, bool addGoStatements)
		{
			// Clean up an invalid characters
			// pull apart the file passed in and remove any funky characters
			string directory = Path.GetDirectoryName(fileName);
			string fileOnly = Path.GetFileName(fileName);

			fileOnly = CleanUpFileName(fileOnly);

			fileName = Path.Combine(directory, fileOnly);

			//if (File.Exists(fileName))
			//    File.Delete(fileName);

			
			if (!Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			TextWriter tw = new StreamWriter(fileName);
			foreach (string s in script)
			{
				string stringToWrite = s;
				if (!stringToWrite.EndsWith(Environment.NewLine))
					stringToWrite += Environment.NewLine;

				tw.Write(stringToWrite);
				//if (s.StartsWith("SET QUOTED_IDENTIFIER") || s.StartsWith("SET ANSI_NULLS"))
				//    tw.Write(System.Environment.NewLine + "GO");

				if (addGoStatements)
				{
					// if the previous line doesn't endwith a NewLine, write one
					if (!stringToWrite.EndsWith(System.Environment.NewLine))
						tw.Write(System.Environment.NewLine);

					tw.WriteLine("GO" + System.Environment.NewLine);
				}
			}
				
			tw.Close();
			return;
		}

		private static string CleanUpFileName(string fileOnly)
		{
			// remove any \ and replace with an under score.
			// this is mainly done for schemas and objects owned by DOMAIN\User
			fileOnly = fileOnly.Replace(@"\", "_");
			
			// remove any of the characters that are invalid 
			string regexSearch = new string(Path.GetInvalidFileNameChars());
			Regex r = new Regex(string.Format("[{0}]", Regex.Escape(regexSearch)));
			fileOnly = r.Replace(fileOnly, "");

			return fileOnly;
		}

		private static SqlConnection GetConnection(string serverName, string databaseName)
		{
			SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder();
			csb.DataSource = serverName;


			if (USER_NAME.Length > 0)
			{
				csb.IntegratedSecurity = false;
				csb.UserID = USER_NAME;
				csb.Password = PASSWORD;
			}
			else
			{
				csb.IntegratedSecurity = true;
			}

				

			csb.InitialCatalog = databaseName;
			csb.ApplicationName = "ScriptSqlConfig";
			SqlConnection c = new SqlConnection(csb.ConnectionString);
			return c;

		}


		private static string GetHeaderCommentBlock(string title)
		{
			return string.Format(@"
---------------------------------------------
-- {0}
---------------------------------------------
", title);

		}
	}
}
