# Phase 11-F1 BepInEx導入サポート実装報告

実施日: 2026-08-01

## 結果

Phase 11-F1を、BepInEx本体へ書き込まない診断・案内機能として実装した。ConfigEditorは`Mortal.exe`だけでゲームルートを確定でき、BepInExが未導入でも専用サポートタブを利用できる。

## 実装

- ゲームルート確認を`Mortal.exe`中心のLevel 1へ分離
- BepInEx 6 Unity Mono Windows x64公式構成の読み取り専用診断を追加
- 未選択、未導入、一部導入、BepInEx 5、IL2CPP、別major、混在、初回起動前、初期化確認済み、完全性不一致、検査不能を区別
- `doorstop_config.ini`、`winhttp.dll`、BepInEx 6 Unity Mono core DLL群をLevel 2条件として固定
- `BepInEx/config/BepInEx.cfg`と`LogOutput.log`または`LogOutput.txt`を初期化確認に使用
- BepInEx 5、IL2CPP、混在、reparse pointは互換性確認としてオレンジ表示し、種類ごとの理由を表示
- BepInEx 6の別buildは動作未確認と表示しつつPlugin操作を許可
- 動作確認済みbe.692は実行コード15ファイルのSHA-256を照合し、不一致時だけ完全性エラーとしてPlugin操作を停止
- 未選択は灰色、通常の未導入・初回起動待ちは青、確認済みは緑、互換性確認はオレンジ、完全性不一致・検査不能は赤に分離
- 通常画面は「選ぶZIP名」と次操作を中心にし、技術的な検出根拠を「詳しい検出内容」へ分離
- 導入サポートタブ、公式ガイド、ゲーム／ダウンロードフォルダ、再確認、初回起動、Plugin操作への導線を追加
- BepInEx前提を満たさない場合はPlugin導入とcfg編集を無効化し、installer側でも再検査して書き込みを拒否

## 公式情報の確認

2026-08-01時点のBepInEx公式Unity Mono導入ガイドと公式build一覧を確認した。対象表記は`Unity.Mono-win-x64`。動作確認済みbuild 692と最新build 785の公式ZIPを一時領域へ取得し、展開・実行せずentry一覧だけを確認した。両buildの必須root loaderとcore DLL構成が一致することを確認し、確認後にZIPを削除した。

- 公式ガイド: https://docs.bepinex.dev/master/articles/user_guide/installation/unity_mono.html?tabs=tabid-win
- 公式build一覧: https://builds.bepinex.dev/projects/bepinex_be

UIとREADMEで使用するURLは`BepInExSupportInfo.OfficialGuideUrl`と同じ公式ガイドへ統一した。

## 自動テスト

- ConfigEditor Release build: 成功、警告0、エラー0
- ConfigEditor tests（payloadなし）: 41件成功
- ConfigEditor tests（承認済みPlugin内蔵）: 42件成功
- 追加確認: BepInExなしのLevel 1、状態モデル、案内文字数と公式URL、前提不足時のPlugin書き込み拒否
- 公式be.692 ZIPを使った構成・product version・15ファイルSHA-256の統合確認: 成功
- self-contained単一EXE publish: 成功、出力1ファイル
- 文言・状態整理後のpublish EXE: 51,659,720 bytes / SHA-256 `1F64D92369978FE810B0F9284A58ADFD524CB5898233F308A01EF882A03622E6`
- 出力先: `tools/LOM_MessageSpeed.ConfigEditor/bin/Release/phase11-f1-status-publish/LOM_MessageSpeed.ConfigEditor.exe`
- 既存publish先EXEは使用中で上書きできなかったため変更せず、新しい検証用出力先を使用

## 未実施

- 実ゲームルートを使ったBepInEx初回起動と再確認
- DPI 100% / 150%の実画面確認
- スクリーンリーダーによる手動確認
- clean Windows確認

commit、push、tag、Releaseは行っていない。Phase 11-F2のZIP自動検証・展開補助も開始していない。

## Phase 11-F1.1 公開前実装（2026-08-03）

- 通常表示を「このツールで動作確認したもの」「現在入っているもの」「正常動作時は継続可」「問題時の入れ直し手順」へ簡素化
- 未導入と部分導入を青、検出済みの互換性注意を橙、確認済みbe.692 SHA-256不一致と検査不能を赤に整理
- version取得不能を`BepInEx（versionを確認できません）`として表示
- BepInEx 6の別buildだけではPlugin操作を禁止しない既存安全判定を維持
- reparse point、確認済みbe.692 SHA-256不一致、検査不能ではPlugin操作を禁止
- 技術情報を既定で閉じた「問題報告用の詳細情報」へ分離し、ユーザープロファイルpathを`%USERPROFILE%`へ伏せる処理を追加
- 再導入Gist正本`docs/bepinex-reinstall-guide.md`を作成
- 公式be.692 ZIPを公式配布元から再取得し、ファイル名、entry、SHA-256 `9A3472F5EEFB35A84AE8C6DEA16814B728AFF807C67C14FBFD448E20112951A6`を照合
- 公開前はConfigEditorのGist URLを未設定とし、仮URLを使用しなかった
- 100% / 150%相当のUI renderで主情報、ボタン、折りたたみが一画面に収まることを確認

## Phase 11-F1.1 公開・最終検証（2026-08-03）

ユーザーの公開前最終承認後、次のPublic Gistを作成した。

| 項目 | 値 |
|---|---|
| URL | `https://gist.github.com/d-fukagawa/7557dd9f2128d2ac59fec677a31541f1` |
| Gist ID | `7557dd9f2128d2ac59fec677a31541f1` |
| owner | `d-fukagawa` |
| public | `true` |
| 作成時刻 | `2026-08-03T02:48:48Z` |
| 初回commit | `9c9593e11422c305b3796f4d676401aace0e0e2f` |
| ファイル | `BepInEx-6-Unity-Mono-win-x64-reinstall-guide.md` |
| ローカル正本SHA-256 | `EA23B8E45DFEDB5DDD0A66090D092AA050A66C114CEF271C6C6FD666E3184EA8` |
| 改行正規化後SHA-256 | `5B55D274E7B7D0BFC9BBF0299B482F89CD3A31E1C4A49F6303CA07735014DC6B` |

- 認証なしHTTPアクセスでGist `200 OK`、本文見出しとファイル名を確認
- GitHub APIと`gh gist view`でowner、公開属性、履歴、本文一致を確認
- Gist内の公式ZIPリンクを認証なしで確認し、`200 OK`、`application/zip`、645,702 bytesを確認
- ConfigEditor、README、Phase 12 README／blog／SNS文書へ同一URLを反映
- ConfigEditorにはHTTP clientやdownload APIを追加せず、確認後に既定ブラウザでGistを開く処理だけを実装

最終検証:

- ConfigEditor payloadなし回帰: 42件成功
- 承認済みPlugin内蔵回帰: 43件成功
- 公式BepInEx be.692構成・version・15ファイルSHA-256統合確認: 43件成功
- Plugin静的回帰: 7件成功
- 100% / 150%相当UI render: 一画面内、Gistボタン有効、詳細情報は既定で閉じた状態を確認
- self-contained win-x64単一EXE publish: 成功、警告0、エラー0、出力1ファイル

最終EXE:

| 項目 | 値 |
|---|---|
| path | `artifacts/phase11-f1.1-config-editor-win-x64/LOM_MessageSpeed.ConfigEditor.exe` |
| byte length | `51659705` |
| SHA-256 | `21F3BC80E671815B2D6A324419565B6E0B09D92F79869E78E7E6BF89B3731FF8` |
| file version | `0.3.0.0` |

本リポジトリのcommit、push、tag、GitHub Releaseは行っていない。Phase 11-F2も開始していない。
