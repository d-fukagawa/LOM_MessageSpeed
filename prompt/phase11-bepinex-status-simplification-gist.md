# Phase 11-F1.1 BepInEx表示簡素化・再導入Gist公開計画

## 1. 目的

BepInEx導入サポート画面を、MODやBepInExの知識がない利用者でも一度で理解できる表示へ整理する。

通常画面では次の4点だけを中心にする。

1. ConfigEditorが動作確認したBepInEx
2. 現在ゲームフォルダに入っているBepInEx
3. 正常に動く場合はそのまま利用できること
4. 正常に動かない場合に開く、作者管理の再導入手順Gist

version、runtime、SHA-256の詳細な診断は内部で維持するが、通常表示へ複数の警告を並べない。SHA-256不一致もマルウェアとは断定しない。

## 2. 承認された外部操作の範囲

この計画の実行時は、BepInEx再導入手順をGitHubの公開Gistとして作成し、初回commitと公開を行う。

含む操作:

- Gist本文のローカルドラフト作成
- ユーザーによる公開前レビュー
- GitHub CLIのGist用認証確認・必要な再認証
- `d-fukagawa`所有の公開Gist作成
- 公開ページ、履歴、未ログイン閲覧の確認
- 公開URLをConfigEditor、README、Phase 12文書へ反映
- Gist修正が必要な場合の追加commit

含まない操作:

- 本リポジトリのcommit、push、tag、GitHub Release
- BepInEx ZIPやDLLのGist添付・再配布
- ConfigEditorによるBepInExのdownload、展開、削除、修復
- Phase 11-F2の開始

Gistは公開後に第三者が閲覧・複製できるため、公開コマンド実行前に本文と公開範囲をユーザーへ提示し、最終確認を得る。

## 3. 通常画面の情報設計

### 3.1 基本表示

```text
[このツールで動作確認したもの]
BepInEx 6.0.0-be.692
Unity Mono / Windows x64

[現在入っているもの]
BepInEx 6.0.0-be.785
Unity Mono / Windows x64

BepInExのバージョンが異なっていても、
LOM_MessageSpeedが正常に動作する場合はそのまま使用できます。

正常に動作しない場合は、BepInExの入れ直しをお試しください。
[入れ直し手順を開く]
```

`BepInEx`、`version`、`Unity Mono`を知らなくても、上下の欄を比較し、次の操作を選べる構成にする。

### 3.2 BepInEx未導入

```text
[このツールで動作確認したもの]
BepInEx 6.0.0-be.692
Unity Mono / Windows x64

[現在入っているもの]
BepInExは見つかりませんでした

LOM_MessageSpeedを使用するにはBepInExが必要です。
[導入・入れ直し手順を開く]
```

未導入は異常や危険ではないため、赤色にしない。

### 3.3 現在versionを取得できない場合

```text
[現在入っているもの]
BepInEx（versionを確認できません）
```

内部例外やファイル名を通常表示へ出さない。詳細情報では取得できなかった項目と理由を表示する。

### 3.4 動作確認済みbe.692のSHA-256不一致

```text
[現在入っているもの]
BepInEx 6.0.0-be.692

ファイルを確認できませんでした。
動作確認済みの内容と一致しないため、公式配布元からの入れ直しをおすすめします。
[入れ直し手順を開く]
```

赤色はこの完全性不一致と、アクセス拒否など検査自体ができない場合だけに使用する。

「マルウェア」「感染」「危険なファイル」とは表示しない。不一致原因として、更新、破損、手動変更、別package、改変などがあり、診断だけでは原因を特定できないためである。

## 4. 表示ステータス

利用者向けステータスは次の3分類へ圧縮する。

| 表示状態 | 色 | Plugin操作 | 主な条件 |
|---|---|---:|---|
| BepInExは見つかりませんでした | 青 | 無効 | BepInEx構成なし、または導入途中 |
| BepInExが見つかりました | 緑または橙 | 条件を満たせば有効 | 必要なplugins pathを安全に確認可能 |
| BepInExのファイルを確認できません | 赤 | 無効 | 確認済みbe.692のSHA不一致、読取不能 |

内部では次の診断事実を維持する。

- BepInExの有無
- 検出したproduct version
- Unity Mono / IL2CPPの特徴
- x64 / x86を確認できる情報
- 必須ファイル不足
- BepInEx 5 / 6
- 混在構成
- reparse point
- be.692実行コードのSHA-256
- 初回起動前 / 後

内部状態は安全なPlugin書き込み判定と問題報告用詳細に使用し、通常画面の文章量を増やさない。

## 5. Plugin操作の方針

- BepInEx 6のbuild番号がbe.692と異なるだけではPlugin操作を禁止しない
- version違いは「動作確認外」として内部詳細へ記録する
- `BepInEx\plugins`をゲームルート内の安全なpathとして確認できない場合は書き込まない
- reparse point、読取不能、確認済みbe.692のSHA不一致では書き込まない
- Unity IL2CPP、BepInEx 5、混在構成は現在入っているものとして表示する
- Pluginが読み込まれない可能性が高い構成では、通常画面を複雑にせず再導入Gistへ案内する
- installer実行直前の再検査は維持する

## 6. 詳細情報

通常の「詳しい検出内容」は「問題報告用の詳細情報」へ名称変更する。

表示候補:

```text
検出version
検出runtime
見つからない必須ファイル
混在を判断したファイル
SHA-256不一致ファイル
reparse point
Plugin操作が有効／無効の理由
```

通常は閉じた状態とする。コピー用ボタンを追加する場合も、ローカルユーザー名を含む完全pathは既定で伏せる。

## 7. 再導入Gist

### 7.1 管理方法

Gistの正本となるMarkdownをリポジトリ内へ先に作成する候補:

```text
docs/bepinex-reinstall-guide.md
```

Gist側ファイル名候補:

```text
BepInEx-6-Unity-Mono-win-x64-reinstall-guide.md
```

Gist description候補:

```text
LOM_MessageSpeed向け BepInEx 6 Unity Mono Windows x64 導入・入れ直し手順
```

公開Gistは`d-fukagawa`アカウントの所有とし、anonymous Gistや第三者アカウントは使用しない。

### 7.2 Gist本文

専門用語を避け、次の順序にする。

1. この手順を使う場面
2. ゲームを完全に終了する
3. Steamから`Mortal.exe`があるゲームフォルダを開く
4. 現在のBepInExを削除せず、ゲームフォルダ外のbackupへ移動する
5. 他Pluginとcfgがbackup内に残っていることを確認する
6. BepInEx公式ページを開く
7. `BepInEx-Unity.Mono-win-x64-6.0.0-be.692+851521c.zip`を選ぶ
8. 配布元URLとZIP SHA-256を確認する
9. ZIPの中身を`Mortal.exe`と同じフォルダへ展開する
10. `Mortal_Data\Managed`へ入れない
11. ゲームを一度起動し、タイトル画面まで進んで終了する
12. ConfigEditorの「状態を再確認」を押す
13. ConfigEditorからLOM_MessageSpeedをインストールする
14. 必要な場合だけ、backupした他Pluginや個別cfgを一つずつ戻す
15. 問題があった場合のrollback

### 7.3 安全条件

- `BepInEx`フォルダ全体を即時削除させない
- `plugins`、`config`、他MODを無条件に削除・上書きさせない
- backup先をゲームルート外へ指定する
- BepInExやゲームのEXE/DLLをGistへ添付しない
- 公式BepInEx HTTPS URLだけを使用する
- 非公式mirrorへfallbackしない
- 管理者権限を前提にしない
- セーブデータの場所とBepInExを混同させない
- hash不一致をマルウェアと断定しない

### 7.4 Gist公開手順

実行時の順序:

1. `docs/bepinex-reinstall-guide.md`を完成
2. リンク、ファイル名、ZIP SHA-256、rollbackをローカルレビュー
3. ユーザーへ公開予定本文を提示し最終承認を得る
4. `gh auth status`を確認
5. 現在無効な`d-fukagawa`認証を、Gist作成に必要な権限で再認証
6. `gh gist create --public`で初回commit・公開
7. Gist ID、owner、公開URL、作成時刻を記録
8. `gh gist view`とブラウザの未ログイン相当で本文を確認
9. Gist上の公式リンクを実際に開き、リンク先とファイル名を確認
10. 誤記があればローカル正本を修正し、`gh gist edit`で追加commit
11. 最終Gist URLをConfigEditor定数とREADMEへ反映

公開後のURLはコードへ一か所だけ定義し、ボタン、README、Phase 12文書で共有する。

## 8. ConfigEditor変更候補

```text
tools/LOM_MessageSpeed.ConfigEditor/BepInExInspector.cs
tools/LOM_MessageSpeed.ConfigEditor/BepInExSupportInfo.cs
tools/LOM_MessageSpeed.ConfigEditor/MainForm.cs
tools/LOM_MessageSpeed.ConfigEditor/PluginInstaller.cs
tools/LOM_MessageSpeed.ConfigEditor.Tests/Program.cs
docs/bepinex-reinstall-guide.md
README.md
prompt/phase12-readme.md
prompt/phase12-blog.md
prompt/phase12-sns.md
analysis/phase11-bepinex-support-report.md
```

`BepInExSupportInfo`へ次を一元定義する。

```text
動作確認version: 6.0.0-be.692
動作確認runtime: Unity Mono / Windows x64
公式導入ページURL
公開済み再導入Gist URL
```

## 9. 自動テスト

### 表示変換

- 未導入
- 導入途中
- 公式be.692・SHA一致
- be.692・SHA不一致
- 別のBepInEx 6 build
- BepInEx 5
- Unity IL2CPP
- 混在構成
- version取得不能
- unreadable
- reparse point

各状態で次を確認する。

- 想定versionが常に表示される
- 現在入っているものが表示される
- version違いだけで赤色にならない
- version違いだけで危険と表現しない
- be.692 SHA不一致は完全性確認として赤色になる
- 未導入は赤色にならない
- Plugin操作の有効・無効が内部安全条件と一致する
- 入れ直しGistボタンが表示される

### URL

- Gist URLが固定HTTPS URL
- ownerが`d-fukagawa`
- 想定したGist ID以外を開かない
- URLを開く前に外部サイト確認を表示
- ConfigEditorがGist本文やBepInExをdownloadしない

### 回帰

- 承認済みPluginの新規導入
- 確認済み旧版Plugin更新
- cfg保存
- ゲーム起動
- payloadなしbuild
- 承認済みpayload内蔵build
- self-contained単一EXE publish

## 10. 手動確認

- DPI 100% / 150%で4項目が一画面に収まる
- 長いversion文字列でもレイアウトが崩れない
- キーボードだけでGistボタンへ移動できる
- スクリーンリーダーが「想定」「現在」「次の操作」を区別できる
- 公開GistをGitHub未ログイン状態で閲覧できる
- スマートフォン幅でもGist手順を読める
- Gistから公式ページへ移動できる
- backupとrollback手順が初心者に誤解されない

## 11. 実行順序

```text
1. UI文言と表示モデルを実装
2. Gist正本Markdownを作成
3. 自動テスト
4. ローカルUI・手順レビュー
5. ユーザーへGist公開前レビューを依頼
6. GitHub再認証
7. 公開Gistを初回commit・公開
8. 公開内容と外部リンクを確認
9. Gist URLをConfigEditorと文書へ固定
10. URLテスト、回帰テスト、単一EXE publish
11. 実装報告、EXE path、SHA-256、Gist URLを提示
```

Gist公開前にURLをコードへ仮置きしない。公開後に確定したURLを使用して最終buildする。

## 12. 完了条件

- 通常画面が「想定」「現在」「正常なら継続」「問題時のGist」の4点に整理されている
- BepInExのversion違いとSHA-256不一致を混同しない
- BepInEx 6の別buildだけを理由に危険表示しない
- 現在入っているBepInExを取得可能な範囲で表示する
- 技術詳細は問題報告用表示へ分離されている
- 再導入手順が`d-fukagawa`所有の公開Gistとしてcommit・公開されている
- Gistが公式ファイルだけを案内し、backupとrollbackを含む
- Gist URLがConfigEditorとREADMEで一致する
- ConfigEditorはGistやBepInExをdownloadしない
- 自動テスト、手動UI確認、self-contained単一EXE publishが成功する
- 最終EXE SHA-256とGist URLが実装報告へ記録される
- 本リポジトリのcommit、push、tag、Releaseは行わない

## 13. 現在の実行前ブロッカー

2026-08-01時点でGitHub CLIには`d-fukagawa`アカウントが登録されているが、tokenが無効である。Gist公開工程では再認証が必要となる。

再認証は公開直前に行い、Gist作成に必要な権限だけを使用する。認証情報をログ、Gist、リポジトリへ保存しない。
