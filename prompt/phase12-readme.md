# Phase 12-A: README更新計画

## 目的

ConfigEditor `0.3.0`から導入フローが変わり、利用者がプラグインZIPを別途展開しなくても、承認済みの文字送り専用`LOM_MessageSpeed 0.2.0`を設定ツールからオフライン導入できるようになったことをREADMEへ正確に反映する。

READMEだけを読んだ新規利用者が、必要なファイル、BepInExの前提、初回導入、設定変更、更新、ゲーム起動、トラブル時の安全な対処を迷わず理解できる状態を目指す。

commit、push、tag、GitHub ReleaseはPhase 13で行い、Phase 12では文書と配布前レビューだけを行う。

## 1. 前提として固定する内容

### 配布候補

```text
ConfigEditor: 0.3.0
内蔵Plugin: LOM_MessageSpeed 0.2.0（文字送り専用）
対象OS: Windows x64
配布方式: self-contained単一EXE
ネットワーク取得: なし
.NET Runtime別途導入: 不要
BepInEx本体の自動導入: なし
```

### 導入先

```text
<GameRoot>\BepInEx\plugins\LOM_MessageSpeed\LOM_MessageSpeed.dll
```

### 自動更新対象

- 未導入環境への新規インストール
- 確認済み正式版`0.1.0`から`0.2.0`への更新
- 同じ`0.2.0`かつ承認済みhashの場合は無変更

次は自動上書きしない。

- 同じversionでもhashが異なるDLL
- 実験版、未知版、破損・読取不能DLL
- 導入対象より新しい版
- target配置とflat配置の重複
- ゲーム起動中または起動状態不明
- junction、symbolic link、その他reparse pointを含む導入経路

## 2. README構成の再整理

README冒頭から利用開始までを次の順序へ整理する。

1. ツールとプラグインの概要
2. 必要環境
3. ダウンロードするファイル
4. 初回導入手順
5. 既存`0.1.0`利用者の更新手順
6. 文字送り設定の変更
7. ゲーム起動
8. 安全仕様と自動上書きしないケース
9. 手動インストール方法
10. アンインストール
11. トラブルシューティング
12. SHA-256確認
13. 互換性・既知制限
14. 問題報告、免責、ライセンス

過去版の説明は履歴として必要な範囲だけ残し、新規利用者向けの主導線と混在させない。

## 3. ダウンロード案内

Phase 13で確定するRelease URLとファイル名を差し込めるようにする。

想定ファイル:

```text
LOM_MessageSpeed-ConfigTool-v0.3.0-win-x64.zip
├─ LOM_MessageSpeed.ConfigEditor.exe
├─ README.md
└─ LICENSE
```

重要な説明:

- 通常利用では設定ツールZIPだけをダウンロードすればよい
- プラグインDLLはEXEへ内蔵されている
- 旧来のコアMOD ZIPを別途ダウンロード・展開する必要はない
- ConfigEditorはBepInExなしでも起動でき、ゲームフォルダ確認後に導入サポートを利用できる
- BepInEx 6 `Unity.Mono-win-x64`はPlugin利用前に手動導入が必要
- 一覧では名前に`Unity.Mono-win-x64`と書かれたZIPを選び、`IL2CPP`、`win-x86`、`NET.Framework`は選ばない
- 正常に動作しない場合は、backupとrollbackを含む作者管理の公開Gist `https://gist.github.com/d-fukagawa/7557dd9f2128d2ac59fec677a31541f1`へ案内する
- BepInEx自体はツールへ含まれない
- ツールはオンライン取得、自動更新、テレメトリを行わない

Phase 13前はURLを仮リンクまたは明示的なプレースホルダーにし、存在しないRelease URLを有効リンクとして掲載しない。

## 4. 初回導入手順

次の利用者操作を、画面上の文言と一致させる。

1. ゲームを完全に終了する
2. ConfigTool ZIPを任意のフォルダへ展開し、`LOM_MessageSpeed.ConfigEditor.exe`を起動する
3. 自動検出されたゲームフォルダを確認する。失敗時だけドライブまたは手動パスで指定する
4. 「BepInEx導入サポート」で公式手順を確認し、BepInEx 6 `Unity.Mono-win-x64`を手動導入する
5. ゲームを一度起動・終了し、「状態を再確認」を押す
6. BepInEx確認後に「プラグインをインストール」を押す
7. game root、導入先、導入版・hashを確認して承認する
8. 「コンフィグ編集」で`Enabled`と`SpeedMultiplier`を保存する
9. 「ゲームを起動」を押す

Windows SmartScreen等の警告が出る可能性と、ファイル名・配布元・SHA-256を確認する方法も近接して案内する。

## 5. 既存利用者の更新手順

確認済み`0.1.0`利用者向けに次を明記する。

1. ゲームを終了する
2. 新しいConfigToolを起動する
3. プラグイン欄が「更新可能な確認済み旧版」になっていることを確認する
4. 「プラグインを更新」を押す
5. 更新後に`0.2.0（確認済み）`となることを確認する

更新時の仕様:

- 直前のDLLを`LOM_MessageSpeed.dll.bak`へ1世代保存
- 既存cfgは削除・初期化しない
- `[General] Enabled`と`[Message] SpeedMultiplier`を維持
- cfg内のコメント、未知項目、既存`[Portrait]`項目も保持
- 立ち絵Motion / Transition機能は正式版に含まれない

## 6. 自動上書きしない場合の案内

利用者向けには内部用語を避け、次の方針で説明する。

```text
確認済みではないプラグインDLLが存在する場合、安全のため自動では上書きしません。
```

READMEでは状態ごとの対処を短く整理する。

BepInEx状態は、未導入・初回起動待ちなど通常の案内、version違いなど互換性の注意、確認済みbe.692のSHA-256不一致など完全性の問題を混同しない。version違いだけで危険とは表現せず、SHA-256不一致もマルウェアと断定しない。

| 表示 | 利用者の対応 |
|---|---|
| 確認済みではない同版 / 未知版 | 使用中DLLの入手元を確認し、必要なら手動で退避 |
| 導入対象より新しい版 | 自動で古い版へ戻さない。現在版を継続利用 |
| 重複配置 | 表示された2つのパスを確認し、利用者自身で整理 |
| 破損または読取不能 | ゲームを終了し、権限・lock・ファイル状態を確認 |
| ゲーム起動中 | ゲームを完全終了して再試行 |

自動削除・自動移動を促す表現は避け、バックアップ後の手動整理を案内する。

## 7. 設定とゲーム起動

編集対象を明確に限定する。

```ini
[General]
Enabled = true

[Message]
SpeedMultiplier = 1.5
```

- `Enabled = false`: ゲーム本来の挙動
- `1.0`: ゲーム本来の速度
- `2.0`: 2倍速
- `0.5`: 半速
- 設定範囲: `0.1`～`10.0`

ゲーム起動中は保存・導入を行わないこと、未保存変更がある場合はゲーム起動を拒否することを記載する。

## 8. 手動導入経路

ConfigEditorを使わない利用者向けに手動導入方法を残す。ただし通常導線より後へ置く。

- Phase 13で別途Plugin ZIPを配布する場合だけ、正式なファイル名・URL・hashを掲載
- 配布しない場合は、ConfigToolからの導入を正式手順とし、EXEからpayloadを取り出す方法は案内しない
- ゲームのManagedディレクトリへDLLを入れないことを明記

## 9. SHA-256とセキュリティ説明

Phase 13の最終build後に次を確定する。

- ConfigEditor EXE SHA-256
- ConfigTool ZIP SHA-256
- 内蔵Plugin DLL SHA-256
- 必要なら単体Plugin ZIP SHA-256

READMEのhashはRelease assetと再計算結果が一致してから掲載する。RC用hashを正式版へ流用しない。

## 10. 既存記述の整理

次を削除または履歴節へ移動する。

- `.NET 10 Desktop Runtime`が別途必要という旧`0.1.0`向け説明
- ConfigEditorとコアMODの2 ZIPが通常利用に必須という旧フロー
- `0.2.0`が未承認候補という記述
- Phase 11の開発用説明を利用者向け仕様のように見せる文章
- 不採用実験ZIPを通常利用者へ強く露出する案内

開発者向けpublishコマンドはREADME末尾または別文書へ分離することを検討する。

## 11. レビューと検証

### 内容確認

- 新規利用者がダウンロード対象を一つに絞れる
- BepInExが別途必要と分かる
- .NET Runtimeが不要と分かる
- 初回導入と`0.1.0`更新が混同されない
- 未知DLLを自動上書きしない理由が分かる
- cfgが維持されることが分かる
- 立ち絵機能が正式版に含まれないことが分かる
- Release前の仮URLやRC hashが残っていない

### 表示確認

- GitHub Markdownで見出し、表、コードブロック、リンクが正しく表示される
- Windows pathとPowerShell例がコピー可能
- スマートフォン幅でも長い表が主情報の理解を妨げない
- 日本語表記と製品名、version表記が統一されている

## 12. 成果物

```text
README.md
analysis/phase12-readme-report.md（必要な場合）
```

## 13. 完了条件

- 新導入フローがREADMEの主導線になっている
- 新規導入、`0.1.0`更新、設定変更、ゲーム起動が一続きで説明されている
- ConfigToolだけでPlugin導入できることと、BepInExは別途必要なことが明確
- 安全な拒否状態と利用者の対処が説明されている
- 旧フローと旧Runtime要件による誤解が残っていない
- Phase 13でURLと最終hashだけを確定できる状態になっている
- 再導入Gist URLのplaceholderをPhase 11-F1.1で公開済みURLへ置換できる
- commit、push、tag、Releaseを行っていない
