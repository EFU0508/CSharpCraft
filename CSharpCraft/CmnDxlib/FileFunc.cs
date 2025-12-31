using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace CmnDxlib
{
    /// <summary>
    /// ファイル・リソース関連のユーティリティクラス
    /// セーブデータ保存や埋め込みリソース展開などに使用
    /// </summary>
    public static class FileFunc
    {
        /// <summary>
        /// アプリ専用の LocalApplicationData フォルダを取得する
        /// </summary>
        /// <remarks>
        /// 例:
        /// C:\Users\ユーザー名\AppData\Local\アプリ名
        ///
        /// フォルダが存在しない場合は自動で作成される
        /// </remarks>
        /// <returns>アプリ用 LocalAppData フォルダパス</returns>
        public static string GetAppDataLocalFolder()
        {
            // LocalApplicationData のルート取得
            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            // 実行ファイル名（拡張子なし）をアプリ名として使用
            string appName = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location);
            // AppData\Local\アプリ名
            folder = Path.Combine(folder, appName);
            // フォルダがなければ作成
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            return folder;
        }

        /// <summary>
        /// 埋め込みリソースをファイルとして書き出す
        /// </summary>
        /// <param name="assm">リソースを含む Assembly</param>
        /// <param name="resourcesFileName">完全修飾されたリソース名</param>
        /// <param name="fileName">
        /// 出力ファイル名（空文字の場合はランダム名）
        /// </param>
        /// <returns>
        /// 書き出したファイルのフルパス
        /// （失敗時は空文字）
        /// </returns>
        public static string ResourceToFile(Assembly assm, string resourcesFileName, string fileName = "")
        {
            string fullPathName = string.Empty;

            try
            {
                byte[] data;

                // 埋め込みリソースをストリームとして取得
                using (Stream stream = assm.GetManifestResourceStream(resourcesFileName))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        // リソース全体を byte 配列として読み込む
                        data = reader.ReadBytes((int)stream.Length);
                    }
                }

                // 出力先フォルダ（LocalAppData\アプリ名）
                string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string appName = Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location);
                folder = Path.Combine(folder, appName);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                // ファイル名指定がなければランダム名
                if (fileName.Length == 0)
                {
                    fullPathName = Path.Combine(folder, Path.GetRandomFileName());
                }
                else
                {
                    fullPathName = Path.Combine(folder, fileName);
                }

                // ファイルとして書き出し
                using (Stream stream = File.OpenWrite(fullPathName))
                {
                    using (BinaryWriter writer = new BinaryWriter(stream))
                    {
                        writer.Write(data);
                    }
                }
            }
            catch (Exception ex)
            {
                // デバッグ出力のみ（アプリは落とさない）
                Debug.WriteLine(ex.Message);
            }

            return fullPathName;
        }

        /// <summary>
        /// ushort の 3 次元配列を高速にバイナリ保存する
        /// </summary>
        /// <remarks>
        /// ・配列サイズ（x,y,z）を先頭に書き込む
        /// ・データ本体はフラット配列化して一括書き込み
        /// ・チャンクデータやボクセル保存向け
        /// </remarks>
        /// <param name="filePath">保存先ファイルパス</param>
        /// <param name="ushorts">保存する 3 次元配列</param>
        public static void SaveFastUshort(string filePath, ushort[,,] ushorts)
        {
            // 各次元サイズ取得
            int xLen = ushorts.GetLength(0);
            int yLen = ushorts.GetLength(1);
            int zLen = ushorts.GetLength(2);

            // 3次元配列を1次元配列にフラット化
            ushort[] flat = new ushort[xLen * yLen * zLen];
            int index = 0;

            foreach (var val in ushorts)
            {
                flat[index++] = val;
            }

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var bw = new BinaryWriter(fs))
            {
                // サイズ情報を書き込む
                bw.Write(xLen);
                bw.Write(yLen);
                bw.Write(zLen);

                // ushort 配列を byte 配列に変換して一括書き込み
                byte[] buffer = new byte[flat.Length * sizeof(ushort)];
                Buffer.BlockCopy(flat, 0, buffer, 0, buffer.Length);
                bw.Write(buffer);
            }
        }

        /// <summary>
        /// SaveFastUshort で保存したファイルを読み込む
        /// </summary>
        /// <param name="filePath">読み込むファイルパス</param>
        /// <returns>復元された ushort の 3 次元配列</returns>
        public static ushort[,,] LoadFastUshort(string filePath)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var br = new BinaryReader(fs))
            {
                // 配列サイズ読み込み
                int xLen = br.ReadInt32();
                int yLen = br.ReadInt32();
                int zLen = br.ReadInt32();

                int total = xLen * yLen * zLen;
                // データ本体を一括読み込み
                byte[] buffer = br.ReadBytes(total * sizeof(ushort));
                ushort[] flat = new ushort[total];
                Buffer.BlockCopy(buffer, 0, flat, 0, buffer.Length);

                // 1次元 → 3次元配列に復元
                var blockIds = new ushort[xLen, yLen, zLen];
                int index = 0;
                for (int x = 0; x < xLen; x++)
                {
                    for (int y = 0; y < yLen; y++)
                    {
                        for (int z = 0; z < zLen; z++)
                        {
                            blockIds[x, y, z] = flat[index++];
                        }
                    }
                }

                return blockIds;
            }
        }
    }
}
