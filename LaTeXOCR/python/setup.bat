@echo off
echo Creating a virtual environment...
py -m venv venv || exit /b 1

echo.
echo Installing required packages... This may take a while.
.\\venv\\Scripts\\pip.exe install -r requirements.txt || exit /b 1

echo.
echo Package setup complete!
pause