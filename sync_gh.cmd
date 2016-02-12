@echo off
cls
git status
git checkout sync
git status
git merge master
git push -u bb sync
git checkout master
git status

pause