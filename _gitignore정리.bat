@echo off
chcp 65001 >nul
cd /d "E:\MiningGirl"
echo ==== git 추적 해제 시작 (로컬 파일은 삭제되지 않습니다) ====
git rm -r --cached "Client/MiningGirl/obj"
git rm --cached "Client/MiningGirl/UserSettings/Layouts/default-6000.dwlt"
git rm --cached "Client/MiningGirl/UserSettings/EditorUserSettings.asset"
echo ==== 완료. 아래는 현재 상태입니다 ====
git status
echo.
echo 확인 후 커밋하려면 이 창에서 다음을 실행하세요:
echo    git add .gitignore
echo    git commit -m "chore: obj/UserSettings 추적 해제 및 .gitignore 보강"
pause