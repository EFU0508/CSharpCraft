using System;

namespace CmnDxlib
{
    /// <summary>
    /// ゲーム計算でよく使う処理をまとめたもの
    /// </summary>
    public static class Calc
    {
        /// <summary>
        /// 角度（度）をラジアンに変換する
        /// </summary>
        /// <typeparam name="T">
        /// float / double / int など、数値型を想定
        /// </typeparam>
        /// <param name="degrees">度数法の角度</param>
        /// <returns>ラジアン値</returns>        
        public static T DegreeToRadian<T>(T degrees) where T : struct, IConvertible
        {
            // ジェネリック型を一旦 double に変換して計算する
            double degreesAsDouble = Convert.ToDouble(degrees);
            // ラジアン = 度 × (π / 180)
            double radiansAsDouble = degreesAsDouble * (Math.PI / 180.0);
            // 計算結果を元の型 T に戻す
            return (T)Convert.ChangeType(radiansAsDouble, typeof(T));
        }

        /// <summary>
        /// ラジアンを角度（度）に変換する
        /// </summary>
        /// <typeparam name="T">
        /// float / double / int など、数値型を想定
        /// </typeparam>
        /// <param name="radians">ラジアン値</param>
        /// <returns>度数法の角度</returns>
        public static T RadianToDegree<T>(T radians) where T : struct, IConvertible
        {
            // ジェネリック型を double に変換
            double radiansAsDouble = Convert.ToDouble(radians);
            // 度 = ラジアン × (180 / π)
            double degreesAsDouble = radiansAsDouble * (180.0 / Math.PI);
            // 元の型 T に変換して返す
            return (T)Convert.ChangeType(degreesAsDouble, typeof(T));
        }

        /// <summary>
        /// 数学的な「床除算（floor division）」を行う
        /// </summary>
        /// <remarks>
        /// C# の / 演算子は 0 方向への切り捨てだが、
        /// この関数は「必ず小さい方の整数」に切り捨てる。
        ///
        /// 例:
        ///   -3 / 2 = -1  (C#)
        ///   FloorDiv(-3, 2) = -2
        /// </remarks>
        /// <param name="a">被除数</param>
        /// <param name="b">除数</param>
        /// <returns>床除算の結果</returns>
        public static int FloorDiv(int a, int b)
        {
            int div = a / b;    // 通常の整数除算
            int rem = a % b;    // 余り
            // 余りがあり、かつ符号が異なる場合は 1 引く
            // (負の方向へ切り下げるため)
            if (rem != 0 && ((a ^ b) < 0))
                div--;
            return div;
        }

        /// <summary>
        /// 数学的に正しい剰余（常に 0 ～ b-1 の範囲）
        /// </summary>
        /// <remarks>
        /// C# の % は負数を扱うと負の値になることがあるため、
        /// インデックス計算やループ用途ではこちらを使う。
        ///
        /// 例:
        ///   -1 % 5 = -1   (C#)
        ///   Mod(-1, 5) = 4
        /// </remarks>
        /// <param name="a">値</param>
        /// <param name="b">法（正の数を想定）</param>
        /// <returns>0 ～ b-1 の範囲の剰余</returns>
        public static int Mod(int a, int b)
        {
            int rem = a % b;
            // 負の場合は b を足して正の範囲に補正
            return (rem < 0) ? rem + b : rem;
        }

    }
}
