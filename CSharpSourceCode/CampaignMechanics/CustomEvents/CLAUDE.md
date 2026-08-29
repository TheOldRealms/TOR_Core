# CampaignMechanics/CustomEvents

A generic scripted-event framework: named events with a frequency tier, cooldown, a
condition predicate, and a consequence action — a lightweight alternative to writing a
full `CampaignBehaviorBase` for every one-off flavor event.

- **`CustomEvent`** — `(StringId, CustomEventFrequency, Cooldown, Func<bool> condition,
  Action consequence)`; `DoesConditionHold()`/`Trigger()`. `CustomEventFrequency`:
  Rare/Uncommon/Common/Abundant/Special.
- **`CustomEventsCampaignBehavior`** (`: CampaignBehaviorBase`) — periodically rolls
  registered `CustomEvent`s against their frequency/cooldown and fires the ones whose
  condition holds.
- **`SimpleCareerQuestBehavior`** (`: CampaignBehaviorBase`) — lightweight one-off
  Career-flavor quests built on this same event pattern rather than a full `Quests/` quest class.
