
[![Azure DevOps builds](https://img.shields.io/azure-devops/build/stevebilogan/3cec9a89-b91e-4fd3-baac-339495363ef1/2)](https://stevebilogan.visualstudio.com/Daily%20Reflection/_build?definitionId=2)
[![Azure DevOps tests](https://img.shields.io/azure-devops/tests/stevebilogan/Daily%2520Reflection/2)](https://stevebilogan.visualstudio.com/Daily%20Reflection/_testManagement/runs?_a=runQuery)
<img src="https://github.com/kazo0/DailyReflection/blob/master/images/feature-graphic.png">

Daily excerpts from a book of reflections by A.A. members for A.A. members


https://apps.apple.com/us/app/aa-daily-reflection/id1536494178#?platform=iphone


https://play.google.com/store/apps/details?id=com.kazo0.dailyreflection&hl=en_CA&gl=US

## Uno Platform port

The Uno Platform head (`DailyReflection/`, Uno single project) ships with `ApplicationId = com.kazo0.dailyreflection` so the store listings above upgrade the original Xamarin.Forms app (3.4/34) in place. On first launch after the upgrade, user settings (sober date, notification time/enabled, sober-time display preference) are imported from the legacy platform stores — Android SharedPreferences / iOS `DR_Settings` NSUserDefaults suite — and the daily notification is re-scheduled. See `specs/001` and `specs/011` for details.
