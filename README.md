Script SQL Server Configuration
===============
Script SQL Server configuration information in a format suitable for DR purposes or checking into a source control system.

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
