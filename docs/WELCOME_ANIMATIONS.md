# Post-login welcome animations

BDFR LogonUI includes nine selectable vector welcome animations.

They are rendered with WPF primitives and animation timelines rather than pre-rendered video files.

## Presets

1. Persian Sunrise — طلوع ایرانی
2. Elegant Fade — محو شدن مینیمال
3. Particle Bloom — شکوفه ذرات
4. Aurora Flow — جریان شفق
5. Glass Panels — پنل‌های شیشه‌ای
6. Typography Wave — موج تایپوگرافی
7. Nature Seasons — چهار فصل
8. Minimal Circle — دایره مینیمال
9. City to Desktop — شهر تا دسکتاپ

## Settings

Open:

**تم، گیج و خوش‌آمد**

The settings window contains a gallery for all nine presets.

You can:

- select a welcome animation;
- enable/disable the post-login animation;
- choose a duration from 1.5 to 8 seconds;
- preview the selected animation before saving.

The selected preset is stored at:

`%LocalAppData%\BDFR\LogonUI\welcome-animation.demo.json`

## Standalone test

The main toolbar contains:

**تست خوش‌آمد**

This plays the saved animation in the standalone dashboard.

## Authentication integration

The standalone project now exposes one integration point:

`PlaySuccessfulLoginWelcomeAsync(...)`

When the Credential Provider/authentication milestone is ready, a successful Windows authentication transition should call the same welcome-animation selection rather than implementing a separate animation system.

The current Credential Provider preview does not collect or serialize credentials yet, so the automatic post-password trigger remains intentionally disconnected until that security gate is passed.
