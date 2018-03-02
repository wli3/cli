SET toolpath=c:\work\cli\bin\toolpath%RANDOM%
SET PATH=%PATH%;%toolpath%
dotnet install tool dotnet-sql-cache --tool-path %toolpath% --version 2.1.0-preview1-28181
%toolpath%\dotnet-sql-cache -v