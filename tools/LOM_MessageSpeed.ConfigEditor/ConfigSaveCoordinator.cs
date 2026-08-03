using System;

namespace LOM.MessageSpeed.ConfigEditor
{
    internal sealed class ConfigSaveResult
    {
        internal ConfigSaveResult(bool rebased)
        {
            Rebased = rebased;
        }

        internal bool Rebased { get; }
    }

    internal static class ConfigSaveCoordinator
    {
        internal static ConfigSaveResult Save(
            ConfigDocument loaded,
            string path,
            bool enabled,
            decimal multiplier,
            Func<bool> mayRetryAfterExternalChange)
        {
            try
            {
                loaded.Save(enabled, multiplier);
                return new ConfigSaveResult(false);
            }
            catch (ConfigChangedException)
            {
                if (!mayRetryAfterExternalChange())
                {
                    throw new ConfigChangedException(
                        "設定ファイルの更新を検出しましたが、ゲーム停止状態を再確認できないため保存しません。");
                }

                ConfigDocument latest = ConfigDocument.Load(path);
                latest.Save(enabled, multiplier);
                return new ConfigSaveResult(true);
            }
        }
    }
}
