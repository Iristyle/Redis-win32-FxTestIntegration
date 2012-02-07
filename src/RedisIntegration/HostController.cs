using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Permissions;

namespace RedisIntegration
{
	/// <summary>	A class that disposes the Redis host process properly.  </summary>
	/// <remarks>	8/3/2011. </remarks>
	internal class HostProcessController
	{
		private Process _process;
		
		/// <summary>	Gets the full pathname of the configuration file. </summary>
		/// <value>	The full pathname of the configuration file. </value>
		public string ConfigFilePath { get; private set; }
		
		/// <summary>	Gets the full pathname of the database file. </summary>
		/// <value>	The full pathname of the database file. </value>
		public string DatabaseFilePath { get; private set; }
		
		/// <summary>	Gets the port. </summary>
		/// <value>	The port. </value>
		public int Port { get; private set; }
		
		/// <summary>	Gets the host address. </summary>
		/// <value>	The host. </value>
		public string Host { get; private set; }

		/// <summary>	Finalizer responsible for safely cleaning up the database and configuration files. </summary>
		/// <remarks>	8/3/2011. </remarks>
		[SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Exception type is unimportant for clean-up"), 
		SecurityPermission(SecurityAction.Demand)]
		~HostProcessController()
		{
			_process.Kill();
			try
			{
				File.Delete(DatabaseFilePath);
			}
			catch { }
			try
			{
				File.Delete(ConfigFilePath);
			}
			catch { }
			try
			{
				_process.Dispose();
			}
			catch { }
		}

		/// <summary>	Constructor. </summary>
		/// <remarks>	8/3/2011. </remarks>
		/// <param name="databaseFilePath">	Full pathname of the database file. </param>
		/// <param name="configFilePath">  	Full pathname of the configuration file. </param>
		/// <param name="host">			   	The host. </param>
		/// <param name="port">			   	The port. </param>
		/// <param name="process">		   	The process. </param>
		public HostProcessController(string databaseFilePath, string configFilePath, string host, int port, Process process)
		{
			this.ConfigFilePath = configFilePath;
			this.DatabaseFilePath = databaseFilePath;
			this.Host = host;
			this.Port = port;
			this._process = process;
		}
	}
}