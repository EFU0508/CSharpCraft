using System;
using static DX;

namespace GameLabo
{
    /// <summary>
    /// StClass に関するユーティリティ関数
    /// </summary>
    public static class StClassLib
    {
        /// <summary>
        /// ゲーム開始時（StClass.startCount）からの経過時間をミリ秒で取得する
        /// </summary>
        /// <returns>
        /// ゲーム開始からの経過ミリ秒数
        /// </returns>
        public static int GetBaseCount()
        {
            // DxLib の GetNowCount() は「アプリ起動からの経過ミリ秒」を返す
            // そこからゲーム開始時に記録した startCount を引くことで、
            // 「ゲーム開始からの経過時間」を求めている
            return GetNowCount() - StClass.startCount;
        }
    }
}
