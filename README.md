# Financial Game Proof of Concept

面向大学生的金融决策严肃游戏原型。当前为 **30 天房间与技巧版本**：手绘风格房间、可点击冰箱与日历、六类金融技巧、心情、逐笔记账、每周账单和延迟收入；游戏界面为英文。

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

- 点击右下角桌上的 **Journal**：处理当天事件，完成所有阶段后才能 End Day。17 个固定事件起点，另有条件触发的回访。
- 点击冰箱查看食物；点击墙上 **Calendar** 查看房租、工资、生日及已确认到账日期。
- 点击中间桌上的 **Money Tracker**：查看交易、待到账款项、缓冲目标和决策记录。点击日志内 **Why it matters** 可选读技巧解释。
- 开局资金随机为 $740 / $780 / $820；每周房租 $500、固定工资 $540，普通每日生活费 $30。有食物储备时仅扣其他生活费 $10。
- 心情初始 65。工作降低心情，恢复活动增加心情；连续工作有额外损耗。选项显示实际变化，免费恢复也可用。
- 无法支付生活费、到期房租或心情降至 0 时结束，可用 **New Run** 重开。独立开始菜单尚未实现。
- **Tools > Financial Game > Validate Room Rules**：三个开局共采样 1,500 条随机路线，检查结算与技巧状态。随机路线通关比例约 15%–38%，不是实际玩家通关率；三个开局的规划路线均可通关。
- 刚进入 Play 的第一天可运行 **Validate Room Live UI**，通过真实场景按钮的事件回调完成一条规划路线并检查日历、冰箱、布局、滚动和重开。该测试会替代当前这一局，结束后恢复到新开局。
- 场景中的 UI 已保存并绑定；正常试玩不需要运行生成工具。
- 导出前确认 Build Profiles 的场景列表包含 `MainGame`。

当前玩法、技巧事件、实现边界与参考资料见 [房间与技巧说明](Assets/Documentation/RoomAndStrategies.md)。原有财务与心情规则见 [30 天基础规则](Assets/Documentation/ThirtyDayPrototype.md)。选中 `GameSystems` 后，可在 Finance Rules、Mood System、Game Event Schedule 中调整参数；条件事件由 StrategySystem 管理，房间界面由 Canvas 上的 RoomInterface 管理。

房间插画保存在 `Assets/Art/StudentRoom.png`，生成方式与提示词见 [素材说明](Assets/Art/StudentRoom-README.md)。

旧版事件机制与文献边界保留在 [EventDesign.md](Assets/Documentation/EventDesign.md)，其中十天版数值已被新版替代。

供组员撰写此前 PoC 说明 PDF 的 [原型创作过程与完整玩法说明](docs/原型创作过程与完整玩法说明.md) 是十天版历史记录，不能当作当前版本的功能说明。

选择的是同时包含 `Assets`、`Packages` 和 `ProjectSettings` 的文件夹，不是单独的 `Assets` 文件夹。

## 小组协作

- 组员统一使用上面的 Unity 版本，避免无意升级项目。
- 提交 `Assets`（包括对应的 `.meta` 文件）、`Packages` 和 `ProjectSettings`。
- `.gitignore` 已排除 `Library`、`Temp`、`Logs`、`UserSettings` 等缓存与本机设置。
- 每个功能使用自己的分支；尽量避免同时编辑同一个场景或预制体。
- 提交前检查变更内容，并在同步到其他电脑后验证场景可以打开。

仓库公开可供查看和克隆；直接推送需要仓库协作者权限。
