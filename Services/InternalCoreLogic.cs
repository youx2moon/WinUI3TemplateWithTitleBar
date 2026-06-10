using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;

namespace $safeprojectname$.Services
{
    /// <summary>
    /// 難読化対象：アプリの核心的な判定ロジックと秘匿文字列を保持するクラス
    /// </summary>
    internal static class InternalCoreLogic
    {
        // 文字列隠蔽(HideStrings)の対象となるストアID
        internal const string ID_T = ""; // Monthly
        internal const string ID_N = ""; // Yearly
        internal const string ID_E = ""; // Pro永久
        internal const string ID_S = ""; // Trial

        /// <summary>
        /// 審査用キャンペーンコードの照合を行い、有効であればライセンス詳細を上書きする
        /// </summary>
        public static bool CheckReviewerBackdoor(string inputKey, ref LicenseManager.LicenseDetails details)
        {
            // 期限設定（2026年5月27日まで）
            DateTime campaignExpiry = new DateTime(2026, 1, 1);

            // 期限内、かつ入力されたAPIキーが審査用コードと一致する場合
            if (DateTime.Now < campaignExpiry && inputKey == "MSFT-REV-20260101-PRO")
            {
                details.Status = LicenseManager.LicenseStatus.Pro;
                details.ExpirationDate = campaignExpiry;
                details.DaysLeft = (int)Math.Ceiling((campaignExpiry - DateTime.Now).TotalDays);
                return true;
            }
            return false;
        }

        /// <summary>
        /// ストアIDからライセンス状態を判定する（ロジック難読化対象）
        /// </summary>
        public static LicenseManager.LicenseStatus DetermineStatus(string storeId)
        {
            if (storeId == ID_T || storeId == ID_N || storeId == ID_E)
            {
                return LicenseManager.LicenseStatus.Pro;
            }
            if (storeId == ID_S)
            {
                return LicenseManager.LicenseStatus.Trial;
            }
            return LicenseManager.LicenseStatus.Free;
        }

        /// <summary>
        /// ストアから取得した生のアドオンリストを解析し、ライセンス詳細を決定する（徹底難読化対象）
        /// </summary>
        public static LicenseManager.LicenseDetails AnalyzeLicenses(IReadOnlyDictionary<string, Windows.Services.Store.StoreLicense> addOnLicenses)
        {
            var details = new LicenseManager.LicenseDetails
            {
                Status = LicenseManager.LicenseStatus.Free,
                ExpirationDate = DateTime.MinValue,
                DaysLeft = 0
            };

            foreach (var addOn in addOnLicenses)
            {
                if (!addOn.Value.IsActive) continue;

                // ID判定（リネーム済みのメソッドを呼び出し）
                var status = DetermineStatus(addOn.Key);

                if (status == LicenseManager.LicenseStatus.Pro)
                {
                    details.Status = LicenseManager.LicenseStatus.Pro;
                    details.ExpirationDate = addOn.Value.ExpirationDate.LocalDateTime;
                    TimeSpan ts = details.ExpirationDate - DateTime.Now;
                    details.DaysLeft = ts.TotalDays > 3650 ? 9999 : (int)Math.Ceiling(ts.TotalDays);
                    return details; // Proが見つかれば確定
                }

                if (status == LicenseManager.LicenseStatus.Trial && details.Status != LicenseManager.LicenseStatus.Pro)
                {
                    details.Status = LicenseManager.LicenseStatus.Trial;
                    details.ExpirationDate = addOn.Value.ExpirationDate.LocalDateTime;
                    TimeSpan ts = details.ExpirationDate - DateTime.Now;
                    details.DaysLeft = Math.Max(0, (int)Math.Ceiling(ts.TotalDays));
                    if (details.DaysLeft == 0 && ts.TotalSeconds > 0) details.DaysLeft = 1;
                }
            }

            return details;
        }

        /// <summary>
        /// 現在の状態に対応する表示名を取得する
        /// </summary>
        public static string GetStatusTag(LicenseManager.LicenseStatus status) => status switch
        {
            LicenseManager.LicenseStatus.Pro => "Pro版",
            LicenseManager.LicenseStatus.Trial => "試用版",
            _ => "無料版"
        };

        // エイリアス命名規則
        public static string GetShortAliasId(string pageId)
        {
            string cleanId = pageId.Replace("-", "").ToLower();
            return $"lml-{cleanId.Substring(0, Math.Min(28, cleanId.Length))}";
        }

        // 画像フィット計算ロジック
        public static (double x, double y, int zoom) CalculateCoverFit(double canvasW, double canvasH, double imgW, double imgH)
        {
            double ratioW = canvasW / imgW;
            double ratioH = canvasH / imgH;

            double scale = Math.Max(ratioW, ratioH);
            double coverRatio = scale * 100 + 0.5;

            double x = -(imgW * scale - canvasW) / 2.0;
            double y = -(imgH * scale - canvasH) / 2.0;
            return (x, y, (int)Math.Ceiling(coverRatio));
        }


    }
}