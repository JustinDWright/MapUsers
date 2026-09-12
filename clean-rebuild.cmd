@echo off
REM Clean and rebuild the Blazor Server solution (Windows)

echo Cleaning solution...
dotnet clean

echo Removing bin and obj folders...
for /d /r %%i in (bin,obj) do if exist "%%i" rd /s /q "%%i"

echo Rebuilding solution...
dotnet build

echo Done. You can now run your application.
