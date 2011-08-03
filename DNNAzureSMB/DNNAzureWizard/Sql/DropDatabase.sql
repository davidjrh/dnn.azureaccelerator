--
-- Drops the DotNetNuke database.  (Warning!)
--
-- SQLCMD -U %dbAdminUsr%@%dbServer% -P %dbAdminPwd% -S tcp:%dbServer%.database.windows.net -d master -i -N DropDatabase.sql
--
DROP DATABASE $(dbName);
GO
DROP LOGIN $(dbUsr);
GO
PRINT('DATABASES:')
GO
SELECT ' ' + Name FROM sys.databases
GO
PRINT('LOGINS:')
GO
SELECT ' ' + Name FROM sys.sql_logins
GO
EXIT