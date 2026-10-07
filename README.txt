Лабораторная «Создание начального меню» — готовый Unity-проект

1. Распакуйте архив.
2. Unity Hub -> Add -> Add project from disk -> выберите папку MenuLab.
   (Проект настроен под Unity 2022.3.11f1 LTS.
3. При первом открытии скрипт Assets/Editor/MenuSceneBuilder.cs сам:
   - импортирует картинки из Assets/GameMenu как Sprite;
   - создаёт сцены Assets/Scenes/Menu.unity и Application.unity;
   - собирает Canvas (Scale With Screen Size, 1920x1080), Background,
     MainMenu (StartButton, OptionButton, QuitButton), OptionsMenu с BackButton;
   - настраивает все On Click () и добавляет сцены в Build Settings (Menu = 0, Application = 1).
   Если не сработало автоматически: меню Tools -> Lab -> Build Menu Scene.
4. В окне Game выберите Full HD (1920x1080) и нажмите Play.
   START -> сцена Application, OPTION -> окно опций, круглая кнопка -> назад,
   EXIT -> в консоли «Приложение закрыто».

Скрипт кнопок: Assets/Scripts/MainMenu.cs
