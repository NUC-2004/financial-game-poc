# Financial Game Proof of Concept

面向大学生的金融决策严肃游戏原型。当前仓库是 Unity 项目初始框架，尚未实现游戏玩法。

## 开发环境

- Unity Editor：**6000.3.9f1**
- 模板：**Universal 2D**（URP 2D Renderer）
- 语言：C#
- 初始场景：`Assets/Scenes/SampleScene.unity`

## 打开项目

1. 克隆本仓库，或将仓库下载为 ZIP 后完整解压。
2. 在 Unity Hub 的 Projects 页面使用 Add / Add project from disk，选择本仓库根目录。
3. 使用 Unity 6000.3.9f1 打开，等待首次依赖解析和资源导入完成。
4. 打开 `Assets/Scenes/SampleScene.unity`。

选择的是同时包含 `Assets`、`Packages` 和 `ProjectSettings` 的文件夹，不是单独的 `Assets` 文件夹。

## 小组协作

- 组员统一使用上面的 Unity 版本，避免无意升级项目。
- 提交 `Assets`（包括对应的 `.meta` 文件）、`Packages` 和 `ProjectSettings`。
- `.gitignore` 已排除 `Library`、`Temp`、`Logs`、`UserSettings` 等缓存与本机设置。
- 每个功能使用自己的分支；尽量避免同时编辑同一个场景或预制体。
- 提交前检查变更内容，并在同步到其他电脑后验证场景可以打开。

仓库公开可供查看和克隆；直接推送需要仓库协作者权限。
