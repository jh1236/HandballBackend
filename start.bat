@ECHO OFF
ECHO Please make sure nginx is running, routing port 80 to 5001
timeout /t 30

set repeated_failure=0
set github_token=None
set can_update=0

REM Check if github_token.txt exists and read the token
if exist github_token.txt (
    for /f "delims=" %%i in (github_token.txt) do set github_token=%%i
    set can_update=1
) else (
    ECHO github_token.txt not found, please create one with your github token
    ECHO Updating from github will not work without a github TOKEN
    timeout /t 15
)

:BEGIN
py start.py --no-debug
set EXIT_CODE=%ERRORLEVEL%

ECHO start.py exited with exit code %EXIT_CODE%

if %EXIT_CODE% == 1 (
    set /a repeated_failure+=1
) else (
    set repeated_failure=0
)

if %EXIT_CODE% == 1 goto :RESTART
if %EXIT_CODE% == 2 goto :UPDATE
if %EXIT_CODE% == 3 goto :TEST
if %EXIT_CODE% == 4 goto :PUSH
ECHO Server has been stopped.
goto :END

:RESTART
if %repeated_failure% lss 3 (
    ECHO start.py is restarting... Attempt %repeated_failure%
) else (
    ECHO start.py failed to start after %repeated_failure% attempts, waiting 5 minutes before pulling from github and retrying...
    timeout /t 300
    goto :UPDATE
)
goto :BEGIN

:UPDATE
if %can_update% == 0 (
    ECHO Cannot update from github without a github token, restarting instead...
    goto :BEGIN
)
ECHO Pulling from github...
git pull https://%github_token%@github.com/jh1236/HandballBackend
ECHO Retrying start.py... Attempt %repeated_failure%
goto :BEGIN

:TEST
ECHO running test.py before restart
py test.py
ECHO Retrying start.py... Attempt %repeated_failure%
goto :BEGIN

:PUSH
ECHO commiting and pushing files
git pull https://%github_token%@github.com/jh1236/HandballBackend
git commit -a -m "Automatic Push From Backend"
git push origin
ECHO Retrying start.py... Attempt %repeated_failure%
goto :BEGIN


:END
pause