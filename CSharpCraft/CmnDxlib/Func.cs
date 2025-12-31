using System;
using System.Drawing;
using System.Runtime.InteropServices;
using static DX;

namespace CmnDxlib
{
    /// <summary>
    /// モデル操作・描画補助・時間演出・配列変換などをまとめたクラス
    /// </summary>
    public static class Func
    {
        /// <summary>
        /// VERTEX3D 配列を byte 配列に変換する
        /// </summary>
        /// <remarks>
        /// ・頂点バッファ作成用
        /// ・Marshal を使い、構造体配列をそのままメモリコピー
        /// </remarks>
        /// <param name="vertices">VERTEX3D 配列</param>
        /// <returns>バイナリ化された byte 配列</returns>
        public static byte[] ConvertVertex3DToByteArray(VERTEX3D[] vertices)
        {
            int structSize = Marshal.SizeOf(typeof(VERTEX3D));
            int byteArraySize = structSize * vertices.Length;
            byte[] result = new byte[byteArraySize];

            // GC による移動を防ぐためにピン留め
            GCHandle handle = GCHandle.Alloc(vertices, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                Marshal.Copy(ptr, result, 0, byteArraySize);
            }
            finally
            {
                handle.Free();
            }

            return result;
        }

        /// <summary>
        /// ushort 配列を byte 配列に変換する
        /// </summary>
        /// <remarks>
        /// ・インデックスバッファ作成用
        /// ・BlockCopy により高速変換
        /// </remarks>
        /// <param name="indices">ushort インデックス配列</param>
        /// <returns>byte 配列</returns>
        public static byte[] ConvertUShortArrayToByteArray(ushort[] indices)
        {
            int byteArraySize = indices.Length * sizeof(ushort);
            byte[] byteArray = new byte[byteArraySize];

            Buffer.BlockCopy(indices, 0, byteArray, 0, byteArraySize);

            return byteArray;
        }

        /// <summary>
        /// string.Format 対応の DrawString ラッパー
        /// </summary>
        /// <remarks>
        /// C言語風の DrawFormatString 的な使い方ができる
        /// </remarks>
        public static int DrawFormatString(int x, int y, uint Color, string FormatString, params object[] args)
        {
            return DrawString(x, y, string.Format(FormatString, args), Color);
        }

        /// <summary>
        /// 度数法（Degree）で XYZ 回転を設定する
        /// </summary>
        /// <remarks>
        /// DxLib の回転はラジアン指定のため内部で変換
        /// </remarks>
        /// <param name="MHandle">MV1 モデルハンドル</param>
        /// <param name="ang">回転角（Degree）</param>
        public static void MV1SetRotationXYZDegree(int MHandle, VECTOR ang)
        {
            float rY = Calc.DegreeToRadian(ang.y);
            float rX = Calc.DegreeToRadian(ang.x);
            float rZ = Calc.DegreeToRadian(ang.z);
            MV1SetRotationXYZ(MHandle, VGet(rX, rY, rZ));
        }

        /// <summary>
        /// 値を min～max の範囲に収める
        /// </summary>
        private static float Clamp(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Color を線形補間（Lerp）する
        /// </summary>
        /// <param name="from">開始色</param>
        /// <param name="to">終了色</param>
        /// <param name="t">補間係数（0～1）</param>
        private static Color LerpColor(Color from, Color to, float t)
        {
            t = Clamp(t, 0f, 1f);
            int r = (int)(from.R + (to.R - from.R) * t);
            int g = (int)(from.G + (to.G - from.G) * t);
            int b = (int)(from.B + (to.B - from.B) * t);
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// 時刻に応じて背景色を更新する（昼夜サイクル）
        /// </summary>
        /// <remarks>
        /// ・朝 / 昼 / 夕 / 夜 を段階的に補間
        /// ・TimeSpan を 24 時間周期として使用
        /// </remarks>
        public static void UpdateBackgroundColor(TimeSpan vt)
        {
            Color nightColor = Color.FromArgb(10, 10, 30);
            Color morningColor = Color.FromArgb(135, 206, 250);
            Color noonColor = Color.FromArgb(100, 180, 255);
            Color eveningColor = Color.FromArgb(255, 160, 100);

            double hour = vt.TotalHours % 24.0;

            Color bgColor;

            if (hour >= 5 && hour < 7)
            {
                float t = (float)((hour - 5) / 2.0);
                bgColor = LerpColor(nightColor, morningColor, t);
            }
            else if (hour >= 7 && hour < 12)
            {
                float t = (float)((hour - 7) / 5.0);
                bgColor = LerpColor(morningColor, noonColor, t);
            }
            else if (hour >= 12 && hour < 15)
            {
                bgColor = noonColor;
            }
            else if (hour >= 15 && hour < 17)
            {
                float t = (float)((hour - 15) / 2.0);
                bgColor = LerpColor(noonColor, eveningColor, t);
            }
            else if (hour >= 17 && hour < 18.5)
            {
                float t = (float)((hour - 17) / 1.5);
                bgColor = LerpColor(eveningColor, nightColor, t);
            }
            else
            {
                bgColor = nightColor;
            }

            SetBackgroundColor(bgColor.R, bgColor.G, bgColor.B);
        }

        /// <summary>
        /// TimeSpan 指定版 太陽・月・ライト更新
        /// </summary>
        public static void UpdateDirectionalLight(VECTOR MyPos, int SunHandle, TimeSpan now, int ShadowMapHandle = -1, int MoonHandle = -1)
        {
            DateTime dt = new DateTime(1900, 1, 1).Add(now);
            UpdateDirectionalLight(MyPos, SunHandle, dt, ShadowMapHandle, MoonHandle);
        }

        /// <summary>
        /// 時刻に応じて太陽・月・平行光源・シャドウ方向を更新する
        /// </summary>
        /// <remarks>
        /// ・24時間を 360° として太陽を円運動させる
        /// ・光源方向は太陽位置から逆向きベクトル
        /// </remarks>
        public static void UpdateDirectionalLight(VECTOR MyPos, int SunHandle, DateTime now, int ShadowMapHandle = -1, int MoonHandle = -1)
        {
            float hour = now.Hour;
            float minute = now.Minute;
            float second = now.Second;
            float millisecond = now.Millisecond;

            // 1日（ミリ秒）を 0～1 に正規化
            float msm = hour * 3600000f + minute * 60000f + second * 1000f + millisecond;
            float deg = msm / 86400000f;
            // 太陽の角度（-90° で日の出基準）
            float radians = deg * (float)Math.PI * 2f - (float)Math.PI / 2f;

            // 太陽の位置（円軌道）
            float x = 900f * (float)Math.Cos(radians);
            float y = 900f * (float)Math.Sin(radians);
            float z = 0f;
            VECTOR sunPosition = VGet(x, y, z);
            if (SunHandle > 0)
            {
                DrawBillboard3D(sunPosition, 0.5f, 0.5f, 100f, 0.0f, SunHandle, TRUE);
            }
            if (MoonHandle > 0)
            {
                VECTOR diff = VSub(MyPos, sunPosition);
                VECTOR oppositePos = VAdd(MyPos, diff);
                DrawBillboard3D(oppositePos, 0.5f, 0.5f, 100f, 0.0f, MoonHandle, TRUE);
            }

            // 光源方向（太陽 → 原点）
            VECTOR lightDirection;
            lightDirection.x = -sunPosition.x;
            lightDirection.y = -sunPosition.y;
            lightDirection.z = -sunPosition.z;
            // 正規化
            float len = (float)Math.Sqrt(lightDirection.x * lightDirection.x +
                                   lightDirection.y * lightDirection.y +
                                   lightDirection.z * lightDirection.z);
            lightDirection.x /= len;
            lightDirection.y /= len;
            lightDirection.z /= len;

            if (ShadowMapHandle > 0)
            {
                SetShadowMapLightDirection(ShadowMapHandle, lightDirection);
            }

            SetLightDirection(lightDirection);
        }

        /// <summary>
        /// VECTOR の成分ごとの除算
        /// </summary>
        public static VECTOR VDiv(VECTOR in1, VECTOR in2)
        {
            VECTOR result = new VECTOR();
            result.x = in1.x / in2.x;
            result.y = in1.y / in2.y;
            result.z = in1.z / in2.z;

            return result;
        }

        /// <summary>
        /// VECTOR の成分ごとの乗算
        /// </summary>
        public static VECTOR VMul(VECTOR in1, VECTOR in2)
        {
            VECTOR result = new VECTOR();
            result.x = in1.x * in2.x;
            result.y = in1.y * in2.y;
            result.z = in1.z * in2.z;

            return result;
        }

        /// <summary>
        /// モデルの最小・最大座標（AABB）を取得する
        /// </summary>
        public static int MV1GetModelMinMaxPosition(int Handle, out VECTOR minV, out VECTOR maxV)
        {
            MV1RefreshReferenceMesh(Handle, 0, TRUE, TRUE);
            MV1_REF_POLYGONLIST RefMesh = MV1GetReferenceMesh(Handle, 0, TRUE, TRUE);

            minV = RefMesh.MinPosition;
            maxV = RefMesh.MaxPosition;

            return TRUE;
        }

        /// <summary>
        /// 球体構造体（当たり判定など用）
        /// </summary>
        public struct Sphere
        {
            public VECTOR Center;
            public float Radius;

            public Sphere(float x, float y, float z, float radius)
            {
                Center = new VECTOR { x = x, y = y, z = z };
                Radius = radius;
            }
        }
    }
}