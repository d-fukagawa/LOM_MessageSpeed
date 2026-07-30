# LOM_MessageSpeed

『活俠傳（Legend of Mortal）』の通常会話における文字送り速度を変更する、非公式のBepInExプラグインです。

本プラグインは、ゲームの開発元・販売元、翻訳MOD作者、BepInEx開発者とは無関係です。通常会話の文字送りだけを対象とし、オート待機、入力待機、演出、戦闘を変更しません（もし、影響があればこっそり教えてください）

## 必要環境

- Steam版『活俠傳』
- 対応確認済みゲームバージョン: `release_1.0.5000.13`
- 対応確認済みUnityバージョン: `2020.3.49f1`
- 対応確認済みBepInExバージョン: `6.0.0-be.692`

ゲームやBepInExの更新後は、内部仕様の変更により動作しなくなる可能性があります。

## 配布ZIPのダウンロード

現在の配布ファイルは、このGitHubリポジトリの`release/`フォルダからダウンロードできます。

- [コアMOD `LOM_MessageSpeed-v0.1.0.zip`をダウンロード](https://github.com/d-fukagawa/LOM_MessageSpeed/raw/refs/heads/main/release/LOM_MessageSpeed-v0.1.0.zip)
- [設定エディタ `LOM_MessageSpeed-ConfigEditor-v0.1.0-win.zip`をダウンロード](https://github.com/d-fukagawa/LOM_MessageSpeed/raw/refs/heads/main/release/LOM_MessageSpeed-ConfigEditor-v0.1.0-win.zip)
- [配布ファイル一覧をGitHubで開く](https://github.com/d-fukagawa/LOM_MessageSpeed/tree/main/release)

通常会話の速度を変更するには、最初の「コアMOD」が必要です。「設定エディタ」は設定変更を補助する任意ツールであり、コアMOD本体を含みません。設定エディタを使う場合も、2つのZIPを別々にダウンロードしてください。

リンクをクリックしても保存が始まらない場合は、配布ファイル一覧から目的のZIPを開き、GitHub画面右上のダウンロードボタンを選択してください。ブラウザがZIPや未署名EXEについて警告した場合は、ファイル名と配布元を確認し、必要に応じて後述のSHA-256確認手順を利用してください。

## インストール

1. ゲームを終了しておいてください
2. Unity Mono版に対応するBepInEx 6を導入します。これは別項目で詳しく案内します。
3. ZIPを展開し、`LOM_MessageSpeed.dll`を次の場所へ配置します。フォルダーがなければ作成してください。

```text
<GameRoot>/BepInEx/plugins/LOM_MessageSpeed/LOM_MessageSpeed.dll
```

4. ゲームを起動します。

ZIPに含まれるDLL以外を、ゲームの`Managed`ディレクトリへ入れないでください。ゲーム本体のDLLをコピー、改変、置換する必要はありません。

## 設定

初回起動後、次の設定ファイルが生成されます。

```text
<GameRoot>/BepInEx/config/lom-messagespeed.cfg
```

設定例:

```ini
[General]
Enabled = true

[Message]
SpeedMultiplier = 1.5
```

- `Enabled = false`: ゲーム本来の挙動
- `SpeedMultiplier = 1.0`: ゲーム本来の速度
- `SpeedMultiplier = 1.5`: 1.5倍速
- `SpeedMultiplier = 2.0`: 2倍速
- `SpeedMultiplier = 0.5`: 半速
- 設定可能範囲: `0.1`～`10.0`
- 実機確認済み範囲: `0.2`～`10.0`

`0.1`は設定可能ですが、問題なく動作することを確認済みとはしていません。設定変更はゲームを完全に終了してから行い、次回起動時に反映してください。

### 設定エディタ（任意）

`LOM_MessageSpeed-ConfigEditor-v0.1.0-win.zip`は、`Enabled`と`SpeedMultiplier`をWindows上で変更するための任意の補助ツールです。コアMODの動作には不要であり、上記のテキストエディタによる設定方法も引き続き正式に対応します。ゲームを完全に終了してから使用してください。

必要環境はWindows 10/11と[.NET 10 Desktop Runtime（Windows x64）](https://dotnet.microsoft.com/download/dotnet/10.0)です。現在のビルド環境に.NET Framework 4.8 Developer Packと.NET 8参照パックがないため、追加SDKを導入せず正式ビルドできる最小の方式として、.NET 10 WinFormsのframework-dependent配布を採用しています。ZIP内の次の4ファイルを同じフォルダへ展開し、`LOM_MessageSpeed.ConfigEditor.exe`を実行してください。

```text
LOM_MessageSpeed.ConfigEditor.exe
LOM_MessageSpeed.ConfigEditor.dll
LOM_MessageSpeed.ConfigEditor.deps.json
LOM_MessageSpeed.ConfigEditor.runtimeconfig.json
```

設定エディタは次の安全条件で動作します。

- 編集対象は選択したゲームルート内の`BepInEx/config/lom-messagespeed.cfg`だけです。
- 管理者権限、ネットワーク通信、テレメトリ、自動更新、常駐処理を使用しません。
- ゲームプロセスの注入、終了、メモリ操作、IPCは行いません。対象ゲームの起動中または起動状態を安全に確認できない場合は保存しません。
- 未知の設定、コメント、順序、改行、UTF-8 BOMを保持し、対象2キーだけを書き換えます。
- 保存時は外部変更を確認し、同じフォルダの一時ファイルから置換して、直前のcfgを`lom-messagespeed.cfg.bak`へ1世代バックアップします。
- cfgがない場合は、一度ゲームを起動して生成させるか、確認画面から最小設定を新規作成できます。

本ツールはコード署名されていません。Windows SmartScreenやセキュリティソフトが警告する可能性があります。配布元に掲示されたSHA-256と手元のファイルを、PowerShellで次のように比較してください。

```powershell
Get-FileHash -Algorithm SHA256 .\LOM_MessageSpeed.ConfigEditor.exe
Get-FileHash -Algorithm SHA256 .\LOM_MessageSpeed-ConfigEditor-v0.1.0-win.zip
```

設定エディタを削除するにはゲームを終了し、展開した上記4ファイルを削除します。EXEだけを削除した時点でも設定エディタは起動できなくなり、残る3ファイルはゲームやコアMODへ作用しません。cfgとコアMODはそのまま利用できます。

設定エディタ固有の問題は[GitHub Issuesの設定エディタ用フォーム](https://github.com/d-fukagawa/LOM_MessageSpeed/issues/new?template=config_editor_bug_report.yml)から報告してください。

## アンインストール

1. ゲームを完全に終了します。
2. 配置した`LOM_MessageSpeed.dll`だけを削除します。

設定ファイル`BepInEx/config/lom-messagespeed.cfg`が残っていてもゲームには影響しません。不要であればユーザー自身で削除できます。ゲーム本体ファイルやセーブデータを復元する必要はありません。

## BepInEx 6 の導入と必要性について

**LOM_MessageSpeedを使用するには、事前にBepInEx 6（Unity Mono版）の導入が必要です。**

LOM_MessageSpeed単体ではメッセージ表示速度を変更できません。BepInExがゲーム起動時にプラグインを読み込み、設定ファイルの生成とHarmonyパッチの適用を行います。

- 必要な種類: **BepInEx 6 Unity Mono版**
- 対応確認済みバージョン: **6.0.0-be.692**
- BepInEx 5: 未対応
- Unity IL2CPP版: 対象外

BepInExは、必ず公式サイトから入手してください。

- [BepInEx 6 Unity Mono公式導入手順](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_mono.html?tabs=tabid-win)
- [BepInEx 6公式ビルド一覧](https://builds.bepinex.dev/projects/bepinex_be)

公式ビルド一覧では、Windows向けの`Unity.Mono`パッケージを選択してください。`Unity.IL2CPP`や`.NET Framework`と書かれたパッケージは、このゲーム向けのBepInExパッケージではありません。

BepInExの基本的な導入手順:

1. Steamライブラリで『活俠傳』を右クリックし、ゲームのインストールフォルダを開きます。
2. BepInEx 6 Unity Mono版のZIPを展開します。
3. ZIPの内容を、`Mortal.exe`があるゲームルートへ配置します。
4. 一度ゲームを起動してから終了します。
5. ゲームルートに`BepInEx/config/`、`BepInEx/plugins/`、`BepInEx/LogOutput.log`が生成されていることを確認します。
6. その後、LOM_MessageSpeedを次の場所へ配置します。

```text
<GameRoot>/BepInEx/plugins/LOM_MessageSpeed/LOM_MessageSpeed.dll
```


公式ガイドでは、Unity Mono用パッケージを選び、ゲーム実行ファイルのあるルートへ展開して初回起動する流れが案内されています。[BepInEx公式Unity Mono導入ガイド](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_mono.html?tabs=tabid-win)
なお、最新のBepInEx 6ビルドは`be.692`より新しくなっています。READMEでは「`be.692`で確認済み」「それ以外は未確認」とするのが安全です。[BepInEx公式ビルド一覧](https://builds.bepinex.dev/projects/bepinex_be)

## 互換性と既知の制限

- LOM JP Font Patch、LOM JP Ruby Prototype、LOM JP String Vault、XUnity Auto Translatorとは、確認した範囲内で共存します。
- LOM_ReadSkipおよびLOM_DetailedDisplayは設計上の競合評価を行っていますが、今回の実機環境では未確認です。
- `Fungus.Writer.writingSpeed`を変更する別のMODとは競合する可能性があります。
- `character`以外のすべての会話種別、すべての対象外UI、すべてのspeedタグ、VO、wait、スキップ経路を網羅確認したものではありません。
- speedタグ区間ではゲーム側が指定した速度を優先し、speed-reset後に通常倍率へ戻る設計です。
- 句読点待機、明示wait、VO待機、入力待機、オート待機は倍率の対象外です。
- ルビ表示、クリックによる即時表示、次行入力、句読点待機は、確認した範囲内で正常に動作しました。
- `SpeedMultiplier = 0.1`で一度だけロード演出から進まない事象がありました。プラグインの例外や警告は確認されず、再現試験は実施していないため、原因は特定していません。

## 問題報告

[GitHub Issues](https://github.com/d-fukagawa/LOM_MessageSpeed/issues) からお願いします。
SNSのDM窓口は後日追記予定です。

問題を報告する前に本プラグインを外し、同じ問題が再現するか確認してください。報告には、可能な範囲で次の情報を含めてください。

- ゲームバージョン
- BepInExバージョン
- LOM_MessageSpeedバージョン
- `Enabled`と`SpeedMultiplier`の値
- 併用MOD一覧と各バージョン
- 再現手順と再現率
- scene、会話種別、該当文章の特徴
- 期待した結果と実際の結果
- `BepInEx/LogOutput.log`

ログは、ユーザー名、ローカルパス、その他の個人情報が含まれていないか確認してから投稿してください。セーブデータ、ゲームDLL、ゲーム素材は公開Issueへ添付しないでください。

## 免責事項

本MODは非公式であり、ゲームの開発元・販売元とは無関係です。利用は自己責任でお願いします。動作、完全性、将来の互換性は保証されません。ゲーム、BepInEx、他MODの更新や組み合わせにより動作しなくなる可能性があります。

適用法令で認められる範囲において、作者はデータ損失、進行不能、その他、本MODの利用から生じた損害について責任を負いません。必要に応じてセーブデータをバックアップしてください。問題が発生した場合は、本MODを外して再現性を確認してください。

## ライセンスと第三者製品

本プロジェクト独自のコードおよび文書は[MIT License](LICENSE)で提供されます。ゲーム本体、BepInEx、Harmony、他のMODおよびそれらの素材・コードは本プロジェクトのライセンス対象ではなく、それぞれの権利者に帰属します。配布ZIPにはゲーム本体、BepInEx、Harmony、翻訳データ、ゲーム素材を含みません。
