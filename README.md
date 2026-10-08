# Financial Game Proof of Concept

面向大学生的金融决策严肃游戏原型。当前已实现 10 天可玩流程、每日事件选择、收支记账和账单安排；游戏界面为英文。

## 开发环境

- Unity Editor：**6000.3.9f1**
- 渲染：**Built-in Render Pipeline**，Canvas / TextMesh Pro 界面
- 输入：Input System **1.18.0**
- 语言：C#
- 游戏场景：`Assets/Scenes/MainGame.unity`

## 打开项目

1. 克隆本仓库，或将仓库下载为 ZIP 后完整解压。
2. 在 Unity Hub 的 Projects 页面使用 Add / Add project from disk，选择本仓库根目录。
3. 使用 Unity 6000.3.9f1 打开，等待首次依赖解析和资源导入完成。
4. 打开 `Assets/Scenes/MainGame.unity`，点击 Play，自动随机开局。

## 试玩与验证

- 点击右下角 **Daily Journal**：第 1、3、5、7、9 天有三选一事件，选完后才能 End Day。
- 点击左下角 **Ledger**：查看交易、未来账单、待到账款项和决策记录。
- 开局资金随机为 $740 / $760 / $780；房租 $500，普通每日生活费 $30。有食物储备时仅扣其他生活费 $10。
- 无法支付生活费或到期房租时结束，可用 **New Run** 重开。独立开始菜单尚未实现。
- 在刚进入 Play 的第一天，运行 **Tools > Financial Game > Validate Event Choices**。验证覆盖三个开局下的 729 种计划选择组合：507 种通关，81 种交租失败，141 种生活费不足。这个比例不是实际玩家通关率。
- 场景中的 UI 已保存并绑定；正常试玩不需要运行生成工具。
- 当前 Build Profiles 尚未配置场景列表；导出前添加 `MainGame`。

事件机制、依据与数值说明见 [EventDesign.md](Assets/Documentation/EventDesign.md)。

供组员撰写 PoC 说明 PDF 的中文资料见 [原型创作过程与完整玩法说明](docs/原型创作过程与完整玩法说明.md)，包含创作过程、全部功能与事件、数值示例、测试、课堂反馈及下一步计划。

选择的是同时包含 `Assets`、`Packages` 和 `ProjectSettings` 的文件夹，不是单独的 `Assets` 文件夹。

## 小组协作

- 组员统一使用上面的 Unity 版本，避免无意升级项目。
- 提交 `Assets`（包括对应的 `.meta` 文件）、`Packages` 和 `ProjectSettings`。
- `.gitignore` 已排除 `Library`、`Temp`、`Logs`、`UserSettings` 等缓存与本机设置。
- 每个功能使用自己的分支；尽量避免同时编辑同一个场景或预制体。
- 提交前检查变更内容，并在同步到其他电脑后验证场景可以打开。

仓库公开可供查看和克隆；直接推送需要仓库协作者权限。
