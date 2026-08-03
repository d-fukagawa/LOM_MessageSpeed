# LOM_MessageSpeed

『活俠傳（Legend of Mortal）』の通常会話における文字送り速度を変更する、非公式のBepInExプラグインです。

ConfigTool `0.3.0`では、文字送り専用Plugin `LOM_MessageSpeed 0.2.0`の導入・更新、設定変更、ゲーム起動を一つの画面から行えます。Plugin DLLはConfigToolへ内蔵されているため、通常利用ではPlugin ZIPを別にダウンロードする必要はありません。

本プロジェクトは、ゲームの開発元・販売元、翻訳MOD作者、BepInEx開発者とは無関係です。変更対象は通常会話の文字送りです。オート待機、入力待機、演出、戦闘を意図的には変更しません。

## 必要環境

- Windows x64
- Steam版『活俠傳』
- BepInEx 6 Unity Mono版
- 対応確認済みゲームバージョン: `release_1.0.5000.13`
- 対応確認済みUnityバージョン: `2020.3.49f1`
- 対応確認済みBepInExバージョン: `6.0.0-be.692`

ConfigToolはself-contained単一EXEです。.NET Runtimeを別途インストールする必要はありません。

ゲームやBepInExの更新後は、内部仕様の変更により動作しなくなる可能性があります。

## ダウンロード

通常利用でダウンロードするものは、次のConfigTool ZIP一つです。

```text
LOM_MessageSpeed-ConfigTool-v0.3.0-win-x64.zip
├─ LOM_MessageSpeed.ConfigEditor.exe
├─ README.md
└─ LICENSE
```

Plugin DLLは`LOM_MessageSpeed.ConfigEditor.exe`へ内蔵されています。旧版のように、コアMOD ZIPと設定エディタZIPを別々にダウンロード・展開する必要はありません。

> [!IMPORTANT]
> ConfigTool `0.3.0`は正式Release前です。Phase 13で次の差し込み欄を、公開済みRelease URLと最終ファイル情報へ置き換えます。存在しない仮URLは掲載していません。

```text
Release URL: {{RELEASE_URL}}
ConfigTool file: {{CONFIG_TOOL_FILE_NAME}}
ConfigTool ZIP SHA-256: {{CONFIG_TOOL_ZIP_SHA256}}
ConfigEditor EXE SHA-256: {{CONFIG_EDITOR_EXE_SHA256}}
```

ConfigToolはコード署名されていません。Windows SmartScreenやセキュリティソフトが警告する場合があります。続行を判断する前に、ファイル名、配布元、後述のSHA-256を確認してください。警告機能そのものを無効にする必要はありません。

## 初回導入

1. ゲームを完全に終了します。Steamでも「停止中」になったことを確認します。
2. ConfigTool ZIPを任意のフォルダへ展開し、`LOM_MessageSpeed.ConfigEditor.exe`を起動します。
3. 「ツール設定」で自動検出されたゲームフォルダを確認します。見つからない場合だけ、ドライブまたは`Mortal.exe`があるフォルダを手動指定します。
4. 「BepInEx導入サポート」を開きます。
5. BepInExがない場合は「導入・入れ直し手順を開く」を押し、案内に従ってBepInEx 6 `Unity.Mono-win-x64`を手動導入します。
6. ゲームを一度起動してタイトル画面まで進み、終了後に「状態を再確認」を押します。
7. 「プラグインの操作へ進む」から「プラグインをインストール」を押します。
8. 確認画面のゲームルート、導入先、導入version、SHA-256を確認して承認します。
9. 「コンフィグ編集」で`Enabled`と`SpeedMultiplier`を保存します。
10. 「ゲームを起動」を押します。

Pluginの導入先は次の固定パスです。

```text
<GameRoot>\BepInEx\plugins\LOM_MessageSpeed\LOM_MessageSpeed.dll
```

ゲームの`Mortal_Data\Managed`には配置しません。ゲーム本体のDLLをコピー、改変、置換する必要もありません。

## BepInExの準備

LOM_MessageSpeedをゲーム起動時に読み込むため、Plugin利用前にBepInExが必要です。BepInEx本体はConfigToolへ含まれず、ConfigToolによる自動ダウンロード、展開、更新、修復、削除も行いません。

動作確認済みの構成:

```text
BepInEx 6.0.0-be.692
Unity Mono / Windows x64
```

- [BepInEx 6 Unity Mono公式導入手順](https://docs.bepinex.dev/master/articles/user_guide/installation/unity_mono.html?tabs=tabid-win)
- [BepInEx 6公式ビルド一覧](https://builds.bepinex.dev/projects/bepinex_be)
- [LOM_MessageSpeed向け BepInEx導入・入れ直し手順](https://gist.github.com/d-fukagawa/7557dd9f2128d2ac59fec677a31541f1)

公式ビルド一覧では、名前に`Unity.Mono-win-x64`と書かれたZIPを選びます。`Unity.IL2CPP`、`win-x86`、`NET.Framework`と書かれたものは使用しません。

BepInEx 6のbuild番号が`be.692`と異なるだけではPlugin操作を禁止しません。LOM_MessageSpeedが正常に動作する場合は、そのまま利用できます。正常に動作しない場合は、現在のPluginとcfgを失わないよう、上記の入れ直し手順に沿ってゲームフォルダ外へバックアップしてから作業してください。

動作確認済み`be.692`を示す実行コードが確認済みSHA-256と一致しない場合は、完全性を確認できないためPlugin操作を停止します。SHA-256不一致だけで、原因やマルウェア感染を断定することはできません。

## 既存0.1.0からの更新

確認済み正式版`0.1.0`は、ConfigToolから`0.2.0`へ更新できます。

1. ゲームを完全に終了します。
2. 新しいConfigToolを起動し、ゲームフォルダを確認します。
3. プラグイン欄が更新可能な確認済み旧版を示していることを確認します。
4. 「プラグインを更新」を押し、表示された現在版、導入版、SHA-256を確認します。
5. 更新後に`0.2.0（確認済み）`と表示されることを確認します。

更新時は、直前のDLLを`LOM_MessageSpeed.dll.bak`へ1世代保存します。既存の`lom-messagespeed.cfg`は削除・初期化せず、次の内容を保持します。

- `[General] Enabled`
- `[Message] SpeedMultiplier`
- コメント、未知項目、順序、改行、UTF-8 BOM
- 既存の`[Portrait]`項目

立ち絵Motion / Transition機能は正式版`0.2.0`に含まれません。既存の`[Portrait]`項目は保持しますが、ConfigToolでは編集しません。

## 文字送り設定

設定ファイル:

```text
<GameRoot>\BepInEx\config\lom-messagespeed.cfg
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

`0.1`は設定可能ですが、問題なく動作することを確認済みとはしていません。

ConfigToolは対象の2キーだけを書き換え、未知の設定やコメントを保持します。cfgがない場合は、確認画面から内蔵された最小テンプレートを新規作成できます。保存時は直前のcfgを`lom-messagespeed.cfg.bak`へ1世代バックアップします。

## ゲーム起動

「ゲームを起動」は、検証済みゲームルートの`Mortal.exe`だけを起動します。

- ゲーム起動中または起動状態を安全に確認できない場合、Plugin導入と設定保存を行いません。
- 未保存の設定変更がある場合、先に保存するまでゲームを起動しません。
- 設定が反映されない場合は、ゲームを完全に終了してから再度保存してください。

## 安全仕様

ConfigToolは次の方針で動作します。

- 管理者権限への自動昇格、ネットワーク取得、テレメトリ、自動更新、常駐処理を使用しない
- ゲームプロセスへの注入、終了、メモリ操作、IPCを行わない
- cfg、ゲーム本体、BepInEx本体、他のMODを自動削除・更新しない
- Plugin DLLのversion、SHA-256、サイズ、PE metadataを確認してから操作する
- ゲームルート内の固定された`BepInEx\plugins`配下だけへ書き込む
- junction、symbolic link、その他のreparse pointを含む導入経路へ書き込まない
- 導入直前と導入後に再検査し、失敗時は元のDLLへのrollbackを試みる

確認済みではないPlugin DLLが存在する場合、安全のため自動では上書きしません。

| 表示・状態 | 対応 |
|---|---|
| 確認済みではない同版／未知版 | 使用中DLLの入手元を確認し、必要ならゲーム外へバックアップして手動整理する |
| 導入対象より新しい版 | 自動で古い版へ戻さない。現在版を継続利用する |
| target配置とflat配置の重複 | 表示された2つのパスを確認し、バックアップ後に利用者自身で整理する |
| 破損または読取不能 | ゲームを終了し、権限、ファイルlock、ファイル状態を確認する |
| ゲーム起動中または状態不明 | ゲームを完全に終了して再試行する |
| reparse pointを含む導入経路 | リンク先へは書き込まない。通常のゲームフォルダを指定する |

## ConfigToolを使わない手動導入

Phase 13で単体Plugin ZIPを正式配布する場合だけ、ここへ正式なファイル名、URL、SHA-256を追加します。

単体Plugin ZIPを配布しない場合は、ConfigToolからの導入が正式手順です。EXEから内蔵Pluginを取り出す方法は案内しません。

手動配置を行う場合も、配置先は次の一か所です。

```text
<GameRoot>\BepInEx\plugins\LOM_MessageSpeed\LOM_MessageSpeed.dll
```

`Mortal_Data\Managed`やBepInExの`core`フォルダへは配置しません。

## アンインストール

1. ゲームを完全に終了します。
2. `<GameRoot>\BepInEx\plugins\LOM_MessageSpeed\LOM_MessageSpeed.dll`だけを削除します。
3. ConfigToolが不要なら、展開した`LOM_MessageSpeed.ConfigEditor.exe`を削除します。

`BepInEx\config\lom-messagespeed.cfg`が残っていても、Pluginを外した後はゲームへ影響しません。不要な場合だけ、内容を確認して利用者自身で削除してください。BepInEx本体、ゲーム本体、セーブデータを変更する必要はありません。

## トラブルシューティング

### BepInExが見つからない

ゲームフォルダに`Mortal.exe`があることを確認し、[BepInEx導入・入れ直し手順](https://gist.github.com/d-fukagawa/7557dd9f2128d2ac59fec677a31541f1)に沿って導入してください。導入後はゲームを一度起動・終了し、「状態を再確認」を押します。

### 「自動導入できません」と表示される

確認済みではないDLL、新しい版、破損・読取不能、重複配置などを検出しています。表示された状態を確認し、既存DLLを即座に削除せず、先にゲームフォルダ外へバックアップしてください。

### ゲーム起動中または状態不明と表示される

ゲームを完全に終了し、Steamでも停止したことを確認してから再試行してください。ConfigToolからゲームプロセスを強制終了することはありません。

### 設定が反映されない

ゲームを完全に終了してから設定を保存し、次回起動時に確認します。`Enabled = true`であること、Pluginが`0.2.0（確認済み）`と表示されることも確認してください。

### SmartScreen等の警告が表示される

ConfigToolは未署名です。ファイル名、GitHubの配布元、SHA-256を確認してください。確認できないファイルは実行せず、正式Releaseから再取得してください。

## SHA-256の確認

Phase 13の最終build後に、Release assetから再計算した次の値を掲載します。RCやローカル検証用の値は正式版へ流用しません。

```text
ConfigTool ZIP SHA-256: {{CONFIG_TOOL_ZIP_SHA256}}
ConfigEditor EXE SHA-256: {{CONFIG_EDITOR_EXE_SHA256}}
内蔵Plugin DLL SHA-256: {{PLUGIN_DLL_SHA256}}
```

PowerShellでダウンロード済みファイルを確認できます。

```powershell
Get-FileHash -Algorithm SHA256 -LiteralPath '.\{{CONFIG_TOOL_FILE_NAME}}'
Get-FileHash -Algorithm SHA256 -LiteralPath '.\LOM_MessageSpeed.ConfigEditor.exe'
```

表示された値が、正式Releaseに掲載された値と一致することを確認してください。

## 互換性と既知の制限

- LOM JP Font Patch、LOM JP Ruby Prototype、LOM JP String Vault、XUnity Auto Translatorとは、確認した範囲内で共存します。
- LOM_ReadSkipおよびLOM_DetailedDisplayは設計上の競合評価を行っていますが、今回の実機環境では未確認です。
- `Fungus.Writer.writingSpeed`を変更する別のMODとは競合する可能性があります。
- `character`以外のすべての会話種別、対象外UI、speedタグ、VO、wait、スキップ経路を網羅確認したものではありません。
- speedタグ区間ではゲーム側が指定した速度を優先し、speed-reset後に通常倍率へ戻る設計です。
- 句読点待機、明示wait、VO待機、入力待機、オート待機は倍率の対象外です。
- ルビ表示、クリックによる即時表示、次行入力、句読点待機は、確認した範囲内で正常に動作しました。
- `SpeedMultiplier = 0.1`で一度だけロード演出から進まない事象がありました。プラグインの例外や警告は確認されず、再現試験は未実施です。

## 問題報告

- [一般的なPluginの問題を報告](https://github.com/d-fukagawa/LOM_MessageSpeed/issues/new?template=bug_report.yml)
- [ConfigToolの問題を報告](https://github.com/d-fukagawa/LOM_MessageSpeed/issues/new?template=config_editor_bug_report.yml)

報告には、可能な範囲で次の情報を含めてください。

- ゲーム、BepInEx、LOM_MessageSpeed、ConfigToolのversion
- `Enabled`と`SpeedMultiplier`の値
- 併用MOD一覧と各version
- 再現手順、再現率、期待した結果、実際の結果
- ConfigToolの「問題報告用の詳細情報」
- `BepInEx\LogOutput.log`の関連部分

ログや詳細情報は、ユーザー名、完全なローカルパス、その他の個人情報が含まれていないか確認してから投稿してください。セーブデータ、ゲームDLL、ゲーム素材、個人用cfg全体は公開Issueへ添付しないでください。

## 免責事項

本MODは非公式であり、ゲームの開発元・販売元とは無関係です。利用は自己責任でお願いします。動作、完全性、将来の互換性は保証されません。ゲーム、BepInEx、他MODの更新や組み合わせにより動作しなくなる可能性があります。

適用法令で認められる範囲において、作者はデータ損失、進行不能、その他、本MODの利用から生じた損害について責任を負いません。必要に応じてセーブデータをバックアップしてください。問題が発生した場合は、本MODを外して再現性を確認してください。

## ライセンスと第三者製品

本プロジェクト独自のコードおよび文書は[MIT License](LICENSE)で提供されます。ゲーム本体、BepInEx、Harmony、他のMODおよびそれらの素材・コードは本プロジェクトのライセンス対象ではなく、それぞれの権利者に帰属します。配布ZIPにはゲーム本体、BepInEx、Harmony、翻訳データ、ゲーム素材を含みません。

## 開発者向けpublish

正式なConfigTool publishでは、承認済みPlugin ZIPをbuild時だけ指定します。ZIPを固定SHA-256、entry名、DLL数、DLL SHA-256、サイズ、assembly/file/product versionで検証し、一致したDLLだけをEXEへ埋め込みます。

```powershell
dotnet publish tools/LOM_MessageSpeed.ConfigEditor/LOM_MessageSpeed.ConfigEditor.csproj `
  -c Release `
  -p:PublishProfile=win-x64 `
  -p:ApprovedPluginZip='<承認済みPlugin ZIPのローカルパス>' `
  -o '<出力先>'
```

公開用buildへゲームDLL、BepInEx、Harmony、個人用cfgを含めないでください。
