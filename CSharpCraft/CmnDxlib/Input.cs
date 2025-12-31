using System;
using System.Collections.Generic;
using System.Linq;
using static DX;

namespace CmnDxlib
{
    /// <summary>
    /// キーボード＋ゲームパッドの入力を統合管理するクラス
    /// </summary>
    /// <remarks>
    /// ・DxLib の GetHitKeyStateAll をベースに入力を管理
    /// ・ゲームパッド入力を仮想的にキーボードキーへ割り当て
    /// ・押した瞬間 / 押しっぱなし の判定が可能
    /// </remarks>
    public class Input
    {
        /// <summary>
        /// 入力モード切り替えフラグ
        /// true  : 右スティックを WASD として扱う
        /// false : 左スティックを方向キーとして扱う
        /// </summary>
        private bool mode;

        /// <summary>
        /// キーの状態管理用構造体（※現在は未使用）
        /// 押し始め時間や長押し判定拡張用
        /// </summary>
        private struct KeyState
        {
            public int startTime;       // 押し始めた時間
            public bool isHeldFlag;     // 押しっぱなしフラグ

            KeyState(int startTime, bool isHeldFlag)
            {
                this.startTime = startTime;
                this.isHeldFlag = isHeldFlag;
            }
        };

        /// <summary>
        /// DxLib が扱うキーの最大数
        /// </summary>
        private const int KEY_NUM = 256;
        /// <summary>
        /// 現在フレームのキー状態
        /// </summary>
        private byte[] currentKeyStates;
        /// <summary>
        /// 前フレームのキー状態
        /// </summary>
        private byte[] previousKeyStates;

        /// <summary>
        /// ゲームパッドのデジタル入力状態
        /// </summary>
        public int joypadState;
        /// <summary>
        /// 左スティック（X,Y）/ 右スティック（X,Y）のアナログ値
        /// </summary>
        public int JoypadXBuf1, JoypadYBuf1;
        public int JoypadXBuf2, JoypadYBuf2;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="InputMode">
        /// true  : 右スティックを WASD 入力として使用  
        /// false : 左スティックを方向キーとして使用
        /// </param>
        public Input(bool InputMode = true)
        {
            // 全キー未入力で初期化
            currentKeyStates = Enumerable.Repeat((byte)0, KEY_NUM).ToArray();
            previousKeyStates = Enumerable.Repeat((byte)0, KEY_NUM).ToArray();
            mode = InputMode;
        }

        /// <summary>
        /// 管理リソース解放
        /// </summary>
        public void Dispose()
        {
            currentKeyStates = null;
            previousKeyStates = null;
        }

        /// <summary>
        /// 入力状態を更新する（毎フレーム呼び出す）
        /// </summary>
        public void Update()
        {
            // 前フレームの状態を保存
            Array.Copy(currentKeyStates, previousKeyStates, currentKeyStates.Length);
            // キーボード状態取得
            GetHitKeyStateAll(currentKeyStates);
            // ゲームパッド入力取得
            joypadState = GetJoypadInputState(DX_INPUT_PAD1);
            GetJoypadAnalogInput(out JoypadXBuf1, out JoypadYBuf1, DX_INPUT_PAD1);
            GetJoypadAnalogInputRight(out JoypadXBuf2, out JoypadYBuf2, DX_INPUT_PAD1);

            // モード別 スティック → キー割り当て
            if (mode)
            {
                // 十字キー入力を方向キーへ合成
                currentKeyStates[KEY_INPUT_UP] |= (byte)(joypadState & PAD_INPUT_UP);
                currentKeyStates[KEY_INPUT_DOWN] |= (byte)(joypadState & PAD_INPUT_DOWN);
                currentKeyStates[KEY_INPUT_LEFT] |= (byte)(joypadState & PAD_INPUT_LEFT);
                currentKeyStates[KEY_INPUT_RIGHT] |= (byte)(joypadState & PAD_INPUT_RIGHT);
                // 右スティックを WASD として扱う
                if (Math.Abs(JoypadXBuf2) > Math.Abs(JoypadYBuf2))
                {
                    if (JoypadXBuf2 > 0)
                    {
                        currentKeyStates[KEY_INPUT_D] = 1;
                    }
                    if (JoypadXBuf2 < 0)
                    {
                        currentKeyStates[KEY_INPUT_A] = 1;
                    }
                }
                else if (Math.Abs(JoypadXBuf2) < Math.Abs(JoypadYBuf2))
                {
                    if (JoypadYBuf2 > 0)
                    {
                        currentKeyStates[KEY_INPUT_S] = 1;
                    }
                    if (JoypadYBuf2 < 0)
                    {
                        currentKeyStates[KEY_INPUT_W] = 1;
                    }
                }
            }
            else
            {
                // 左スティックがニュートラルの時のみ十字キーを反映
                if (JoypadXBuf1 == 0)
                {
                    currentKeyStates[KEY_INPUT_LEFT] |= (byte)(joypadState & PAD_INPUT_LEFT);
                    currentKeyStates[KEY_INPUT_RIGHT] |= (byte)(joypadState & PAD_INPUT_RIGHT);
                }
                if (JoypadYBuf1 == 0)
                {
                    currentKeyStates[KEY_INPUT_UP] |= (byte)(joypadState & PAD_INPUT_UP);
                    currentKeyStates[KEY_INPUT_DOWN] |= (byte)(joypadState & PAD_INPUT_DOWN);
                }
            }

            // ボタン入力 → キー割り当て
            if (joypadState != 0)
            {
                // PAD_INPUT_C HORI(Aボタン) XBOX(Xボタン)
                currentKeyStates[KEY_INPUT_X] = (byte)((joypadState & PAD_INPUT_C) != 0 ? 1 : 0);
                // PAD_INPUT_B HORI(Bボタン) XBOX(Bボタン)
                currentKeyStates[KEY_INPUT_Z] = (byte)((joypadState & PAD_INPUT_B) != 0 ? 1 : 0);
                // PAD_INPUT_X HORI(Xボタン) XBOX(Yボタン)
                currentKeyStates[KEY_INPUT_V] = (byte)((joypadState & PAD_INPUT_X) != 0 ? 1 : 0);
                // PAD_INPUT_A HORI(Yボタン) XBOX(Aボタン)
                currentKeyStates[KEY_INPUT_C] = (byte)((joypadState & PAD_INPUT_A) != 0 ? 1 : 0);
                // PAD_INPUT_START HORI(-ボタン) XBOX(Ltoggle)
                currentKeyStates[KEY_INPUT_SPACE] = (byte)((joypadState & PAD_INPUT_G) != 0 ? 1 : 0);
                // PAD_INPUT_M HORI(+ボタン) XBOX(Rtoggle)
                currentKeyStates[KEY_INPUT_RETURN] = (byte)((joypadState & PAD_INPUT_H) != 0 ? 1 : 0);
                // PAD_INPUT_Y HORI(Lボタン) XBOX(Lボタン)
                currentKeyStates[KEY_INPUT_Q] = (byte)((joypadState & PAD_INPUT_Y) != 0 ? 1 : 0);
                // PAD_INPUT_Z HORI(Rボタン) XBOX(Rボタン)
                currentKeyStates[KEY_INPUT_P] = (byte)((joypadState & PAD_INPUT_Z) != 0 ? 1 : 0);
                // PAD_INPUT_L HORI(ZLボタン) XBOX(三ボタン)
                currentKeyStates[KEY_INPUT_1] = (byte)((joypadState & PAD_INPUT_L) != 0 ? 1 : 0);
                // PAD_INPUT_R HORI(ZRボタン) XBOX(呂ボタン)
                currentKeyStates[KEY_INPUT_0] = (byte)((joypadState & PAD_INPUT_R) != 0 ? 1 : 0);
            }
        }

        /// <summary>
        /// キーが「押された瞬間」かどうか
        /// </summary>
        public bool IsKeyPressed(int key)
        {
            return currentKeyStates[key] != 0 && previousKeyStates[key] == 0;
        }

        /// <summary>
        /// キーが「押しっぱなし」かどうか
        /// </summary>
        public bool IsKeyHeld(int key)
        {
            return currentKeyStates[key] != 0;
        }
    }
}
