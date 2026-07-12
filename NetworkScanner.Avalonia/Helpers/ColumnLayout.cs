using System.Globalization;
using System.Linq;
using Avalonia.Controls;

namespace NetworkScanner.Avalonia.Helpers
{
    // DataGrid의 사용자 조절 컬럼 너비를 문자열로 직렬화/복원한다(열 순서 기준).
    internal static class ColumnLayout
    {
        public static string Serialize(DataGrid grid)
        {
            return string.Join(",", grid.Columns.Select(c =>
            {
                double w = c.ActualWidth;
                if (double.IsNaN(w) || w <= 0) w = c.Width.IsAbsolute ? c.Width.Value : 0;
                return ((int)w).ToString(CultureInfo.InvariantCulture);
            }));
        }

        public static void Apply(DataGrid grid, string csv)
        {
            if (string.IsNullOrWhiteSpace(csv)) return;

            string[] parts = csv.Split(',');
            // 저장된 값과 현재 컬럼 수가 다를 수 있으므로(업데이트로 컬럼 추가 등) 최소 개수만큼만 적용한다.
            int n = System.Math.Min(parts.Length, grid.Columns.Count);
            for (int i = 0; i < n; i++)
            {
                if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int w) && w > 0)
                {
                    grid.Columns[i].Width = new DataGridLength(w);
                }
            }
        }
    }
}
