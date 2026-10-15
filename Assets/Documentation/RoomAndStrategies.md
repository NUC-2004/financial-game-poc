# 房间交互与金融技巧版本

## 试玩入口

打开 MainGame，点击 Play 随机开局。游戏仍为 30 天，英文界面。

- 点击冰箱：查看还能吃几天、今天是否已吃过、今晚生活费。
- 点击墙上日历：查看房租、固定工资、生日、已确认收入和主动设置的提醒/愿望单回访。购物、临时加班等尚未遇到的事件不会提前显示。点击日期看详情，左右箭头切换月份。收入先在早晨到账，生活费和房租在 End Day 结算。
- 点击桌上 Money Tracker：查看逐笔现金流水、决策记录、待到账收入和应急缓冲目标。
- 点击右下角 Journal：处理当天事件，再 End Day。多阶段事件必须完成所有阶段才能结束当天。
- 事件正文只保留短情境；可点击 Why it matters 阅读技巧、来源与适用边界。日历和冰箱在日志中也有快捷入口。

房间图片随窗口拉伸铺满，物体按钮使用同一父对象中的相对位置，避免窗口变化后点击区域偏离图片；文字由独立的 TextMesh Pro 组件渲染。极宽或极窄的窗口会改变插画比例。物体及下面的标签都可点击。

字体清晰度检查：Game 预览关闭 Low Resolution Aspect Ratios，Scale 使用 1x；放大 Game 面板后检查实际画面。Canvas 使用屏幕空间叠加、按屏幕尺寸缩放和像素对齐。预览倍率和低分辨率选项属于本机编辑器设置，不会随 Git 同步到同学电脑。

## 六类技巧如何进入玩法

| 技巧 | 首次/主要日期（游戏天数） | 玩家实际操作 |
|---|---|---|
| 比较单位价格 | 1、8、23 | 比较整笔价格、可供食用天数，以及交租前能否承担预付款 |
| 出门设定消费上限 | 2 | 先带 $20 / $40 现金或用卡，再选 $12 午餐、$30 午餐加纪念品、离开；现金上限限制本次消费 |
| 留意饥饿状态 | 4 | 有储备时可先吃当日份额，再在采购环节决定是否加买 $12 零食；吃过的份额晚上不重复消耗 |
| 购买前暂停 | 9，次日跟进 | $30 耳机现在买、列入愿望单、放弃；选择等待后，第 10 天工作事件完成后再考虑购买 |
| 先查库存、列清单 | 15 | 先选择是否查看冰箱和准备四天清单，再选买两天、四天或暂不采购 |
| 为目标设提醒 | 6、可选 16、24 | 设 $40 应急缓冲目标；可让日历在发薪后提醒，再选择记下 $20 / $40 或不预留 |

当前日程有 17 个固定事件起点；部分事件有第二阶段，提醒和愿望单回访由前面选择触发。这是六种技巧的首批实现，尚不是每种技巧三个独立情境的完整事件库。

现金不产生奖励、提现费或隐藏消费。现金上限是明确的原型规则，不等同于复现真实心理效应。饥饿不会强制玩家额外花钱；当前通过情境和可选加购练习识别诱因。等待不保证降价。检查库存/清单主要提供信息帮助。应急缓冲只是可修改的计划，仍包含在余额内，没有真正转入另一账户，也不会锁住资金。因此，这版实现了操作机会，但不能据此证明玩家学会或改变真实消费习惯；后续需观察其是否主动使用这些工具。

## 组件与维护

GameSystems 保留 FinanceRules、MoodSystem、GameEventSchedule、EconomyManager、GameManager，并新增 StrategySystem。Canvas 新增 RoomInterface，负责日历、冰箱和可选说明。房间、按钮和两个新面板都是保存到 MainGame 场景的 Unity UI 组件。

- 固定事件在 GameEventSchedule 的 Events 中调整：Day、Id、Title、Body、Choices、Tip Key。
- 两阶段和条件事件在 StrategySystem.FollowUp / OverrideDay 中定义。现金上限、吃饭、清单、提醒等行为由选项 Action 关联；不要随意改动有代码关联的 Id。
- 当前第二阶段商品价格、目标金额和第 10/16/24 天逻辑在 StrategySystem 中，不是全部 Inspector 可调。修改日程时须同时核对该脚本及 RoomInterface 的相关日历标记。
- 房间 UI 修复菜单：Tools > Financial Game > Install Room and Strategy Events。仅在非 Play 状态使用；正常试玩不必运行。首次迁移增加技巧事件，再次执行保留已有事件数据并重建布局。旧安装菜单会转到本安装器，避免覆盖房间。
- 场景中主界面旧 FoodText 和两个旧导航按钮已禁用，新热点替代它们。
- 仍无跨局存档、随机事件池或不同房间。开局只有资金档位变化，当前以可验证的一月日程为主。

## 验证记录

Unity 6000.3.9f1 编译和规则测试通过。三个开局各采样 500 条随机路线，通关分别为 73、145、188 条；每个开局都有已验证的规划通关路线。这是固定种子下的程序采样，不是玩家测试、全组合枚举或学习效果证据。

Validate Room Rules 检查多阶段事件、重复点击保护、现金上限、食物只消耗一次、余额与流水一致、心情、待到账收入及重开。Validate Room Live UI 在新开局的 Play 模式运行，调用实际场景按钮完成 30 天规划路线，检查日历/冰箱/解释页面切换、文本溢出、账本滚动和重开。会替代当前这局，结束后回到新开局。已人工点击检查房间热点、生日和房租同日的日历显示。

## 资料与设计边界

1. Schomburgk, L., Belli, A., & Hoffmann, A. O. I. (2024). Less cash, more splash? A meta-analysis on the cashless effect. *Journal of Retailing, 100*(3), 382–403. https://doi.org/10.1016/j.jretai.2024.05.003
   支持现金与非现金支付的平均支出差异讨论；不保证个人省钱，也不直接验证本游戏的固定现金上限。
2. Xu, A. J., Schwarz, N., & Wyer, R. S., Jr. (2015). Hunger promotes acquisition of nonfood objects. *Proceedings of the National Academy of Sciences, 112*(9), 2688–2692. https://doi.org/10.1073/pnas.1417712112
   支持饥饿与获取物品倾向的关系；不证明吃饭后必然节省固定金额。
3. Australian Securities and Investments Commission. (n.d.). *Ways to save on food and fuel*. Moneysmart. https://moneysmart.gov.au/budgeting/ways-to-save-on-food-and-fuel
   专业建议支持查库存、列清单和比较单位价格，不能描述为这些游戏机制的因果实验。
4. Australian Securities and Investments Commission. (n.d.). *Avoiding sales pressure*. Moneysmart. https://moneysmart.gov.au/budgeting/avoiding-sales-pressure
   支持购买前留时间评估需要和负担能力；等待一个游戏日是本项目设计。
5. Karlan, D., McConnell, M., Mullainathan, S., & Zinman, J. (2016). Getting to the top of mind: How reminders increase saving. *Management Science, 62*(12), 3393–3411. https://doi.org/10.1287/mnsc.2015.2296
   研究对象为银行储蓄客户；并未验证本游戏对大学生的效果。

金额、食物份额、心情变化和失败条件均为游戏参数。
