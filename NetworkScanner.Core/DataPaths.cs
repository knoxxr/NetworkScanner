using System;
using System.IO;

namespace NetworkScanner
{
    // 번들된 참조 데이터 파일(ouiinfo.ini, reservedports.ini, prohibitports.ini)의 경로를 찾는다.
    // 이 파일들은 실행 파일 옆에 배포되지만, 앱을 실행할 때의 "현재 작업 디렉터리"는 실행 파일 위치와
    // 다를 수 있다(예: macOS .app 번들은 보통 "/"에서 시작, dotnet run은 프로젝트가 아닌 호출 위치).
    // 따라서 작업 디렉터리에 파일이 있으면 그걸 쓰고, 없으면 실행 파일이 있는 디렉터리에서 찾는다.
    internal static class DataPaths
    {
        public static string Resolve(string fileName)
        {
            if (File.Exists(fileName)) return fileName;

            string beside = Path.Combine(AppContext.BaseDirectory, fileName);
            return File.Exists(beside) ? beside : fileName;
        }
    }
}
