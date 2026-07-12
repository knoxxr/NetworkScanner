using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace NetworkScanner
{
    // ListView(GridView)의 사용자 조절 컬럼 너비를 문자열로 직렬화/복원한다(열 순서 기준).
    internal static class ColumnLayout
    {
        public static string Serialize(ListView list)
        {
            if (list.View is not GridView gv) return "";
            return string.Join(",", gv.Columns.Select(c =>
            {
                double w = c.ActualWidth;
                if (double.IsNaN(w) || w <= 0) w = c.Width;
                if (double.IsNaN(w) || w <= 0) w = 0;
                return ((int)w).ToString(CultureInfo.InvariantCulture);
            }));
        }

        public static void Apply(ListView list, string csv)
        {
            if (string.IsNullOrWhiteSpace(csv) || list.View is not GridView gv) return;

            string[] parts = csv.Split(',');
            // 저장된 값과 현재 컬럼 수가 다를 수 있으므로(업데이트로 컬럼 추가 등) 최소 개수만큼만 적용한다.
            int n = Math.Min(parts.Length, gv.Columns.Count);
            for (int i = 0; i < n; i++)
            {
                if (int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int w) && w > 0)
                {
                    gv.Columns[i].Width = w;
                }
            }
        }
    }
}
