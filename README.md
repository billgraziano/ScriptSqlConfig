Script SQL Server Configuration
===============
Script SQL Server configuration information in a format suitable for disaster recovery purposes or checking into a source control system.

Server level objects (linked servers, audits, etc.) are scripted in one file per object type.  This makes DR easier and reviewing history possible.  Database level objects (tables, functions, etc.) are scripted in one file per object.  This makes reviewing object history easy and DR possible.  The database backup is the primary DR mechanism for databases.

The following instance level objects are scripted:

* Logins These are scripted with the proper SID and the hashed password.
* Jobs
* Linked servers
* Audits
* Alerts
* Credentials
* Proxy Accounts
* Database Mail
* Event Notifications
* User-Defined Endpoints

The following instance level objects and current values are written to a file:

* Properties
* Options

User objects in master, model and msdb are scripted.

The following user database objects can be scripted:

* Tables
* Stored procedures
* User-defined data types
* Views
* Triggers
* Table types
* User-defined functions
* Users, Database roles and application roles
* Service Broker Objects

Requirements
------------
This requires a SQL Server installation from SQL Server 2008, 2008 R2, 2012, or 2014.

You can control which version of SMO is loaded by updating the app.config file.

By default it loads the SQL Server 2012 version of SMO.  To change this, uncomment the `<runtime>` section of the app.config file.  You'll see a number of entries that look like this:

      <dependentAssembly>
        <assemblyIdentity name="Microsoft.SqlServer.Smo"
          publicKeyToken="89845dcd8080cc91"
          culture="neutral" />
         Assembly versions can be redirected in application, 
          publisher policy, or machine configuration files. 
        <bindingRedirect oldVersion="11.0.0.0" newVersion="10.0.0.0" />
      </dependentAssembly>

Simply update the `newVersion` attribute with the version of SMO you'd like to target.  A comment in the app.config files includes instructions and the version numbers.    


Notes
-----
Earlier versions of the source code and a 2008 version can be found on CodePlex at https://scriptsqlconfig.codeplex.com/.