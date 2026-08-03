# 更新日志 / Changelog

## v0.9.7-alpha

### 中文

- 移除铲子挖掘后的额外事件与自定义“结束火堆”流程，恢复原版挖掘奖励和原版多人房间切换，以消除黑屏与同步风险。
- 移除 MultiEnchantmentMod 的特殊兼容层，避免兼容异常破坏原版锻造流程。
- 锻造锤现在只在正常锻造未附魔牌后添加一个原版附魔。
- 修复兼容异常导致锻造选项未被消耗、可以在同一营火无限锻造的问题。

### English

- Removed the extra post-Dig event and custom Finish Campfire flow, restoring vanilla Dig rewards and multiplayer room transitions to eliminate black-screen and desync risks.
- Removed the special MultiEnchantmentMod integration to prevent compatibility failures from breaking vanilla Smithing.
- Forging Hammer now adds one vanilla enchantment only after normally Smithing an unenchanted card.
- Fixed the Smith option remaining available indefinitely after a compatibility exception.

## v0.9.6-alpha

### 中文

- 增加独立的“结束火堆”按钮，打开地图不再自动视为结束营火。
- 铲子额外事件会等待所有玩家结束火堆后再触发。
- 非共享铲子事件会等待所有玩家分别完成，期间禁止进入地图或下一房间。
- 修复全员选择下一房间时连续触发两个房间的问题。
- 修复一名玩家仍在处理事件时，其他玩家前进导致的多人黑屏和状态分歧。
- 移除重复发送的营火结束同步消息。
- 明确铲子额外事件不触发活动星图，活动星图继续遵循原版问号房规则。
- 营火选项超过五个时自动缩放并保持居中。
- 新增 `busycampfire_test` 控制台命令，一次添加全部火堆测试遗物。
- 修正锻造锤的原版附魔数值范围。
- 更新中英文遗物说明和营火选项说明。
- 删除不属于 BusyCampfire 的新塔计划、预计伤害、计时与通用便利功能。

### English

- Added a dedicated “Finish Campfire” button; opening the map no longer automatically finishes the rest site.
- Shovel bonus events now wait until every player has finished the campfire.
- Non-shared Shovel events wait for every player to finish before enabling the map or next-room transition.
- Fixed two rooms being triggered when the party selected the next room together.
- Fixed multiplayer black screens and state divergence when one player moved on while another was still resolving the event.
- Removed the duplicate rest-site completion message.
- Shovel bonus events no longer trigger Planisphere; Planisphere continues to follow vanilla unknown-room rules.
- Rest-site options automatically scale and remain centered when more than five are present.
- Added the `busycampfire_test` console command to grant all campfire testing relics at once.
- Corrected Forging Hammer enchantment amounts to use vanilla values.
- Updated English and Chinese relic and rest-site descriptions.
- Removed unrelated Spire planning, incoming-damage, timer and general quality-of-life features.
