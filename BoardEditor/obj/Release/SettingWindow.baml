   M S B A M L   `   `   `  ����   BoardEditor    BoardEditor.SettingWindow   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     X TXceed.Wpf.Toolkit, Version=2.1.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit    � Title$  Настройки��$	��370��$	��400�� � 
ResizeMode$ NoResize=� � ShowInTaskbar .   � WindowStartupLocation$ CenterOwner=� � WindowStyle$ 
ToolWindow=�.���� $��3q�.+��� .@��� $��
Доска��.���  � RowDefinitions �� $	��140 ��� $��40 ��� $��80 � � ColumnDefinitions �� $��60 ��� .+��� $��Center=�$
��Bold"�Шрифт:�� $��1�� �� Orientation$ 
Horizontal=�.+��� -   $��border��
���   �$��1q�$��10q�$	��220��._��� -   $��	tbExample�� �� 
IsReadOnly .  $��Example��	 �� AcceptsReturn	 . $��Stretch=�$��Stretch=�
 �� TextWrapping$

 Wrap=�$��Center=�$��Center=��� -   $��btFont��$��55��$��25��$��Шрифт...���� $��1��$��Center=�$
��Bold"�	Фон:#  Xceed.Wpf.Toolkit.ColorPicker  -     Name$ colorPicker��$��1��$��1��$	��220��$
��Left=�$��	10 5 10 5q��� $��2��$��Center=�$
��Bold"�Размер:� $��2��$��1��$	��220��$��	10 5 10 5q�$
��Left=� �� $��30 ���  �� �� .+��� $��0��$��0��$ 
Horizontal=�.+��� $��Ширина:��$��0 0 10 0q��� ���� *Value+ �� ElementName$ slWidth���� $��0��$��1��$ 
Horizontal=�.+��� $��Высота:��$��0 0 10 0q��� ���� *Value+$ slHeight���� -   $��slWidth��$��1��$��0�� � Minimum$	 400Q� � Maximum$
 1600Q� � LargeChange$ 50Q� � SmallChange$ 10Q� �� TickPlacement$ BottomRight=� �� TickFrequency$ 50Q� �� AutoToolTipPrecision$ 1�� �� Delay$	 100�� �� IsSnapToTickEnabled .  � ToolTip �� *Value+ �� RelativeSource �� *Self+�� -   $��slHeight��$��1��$��1��$	 200Q�$
 1200Q�$ 50Q�$ 10Q�$ BottomRight=�$ 50Q�$ 1��$	 100�� .  �� *Value+ �� *Self+�� $��Соединение��.��� $	��260�� �� $��60 ��� $��40 ���  �� $��60 ��� .+��� $��Center=�$
��Bold"�Адрес:�� $��1��.+��� -   $��	spAddress��$ 
Horizontal=�.+��� $��Center=�$��10q�IP:�� -   $
��cbIp��$��Center=�$	��100��$��10q��� $��Center=�$��10q�Port:�� -	   $��tbPort��$��Center=�$��80��$��10q�$ Порт 1025...65535�� �� 	MaxLength$ 5���� -
   $��tbHint��$��10(�$��10 0 0 0q��� $ 
Horizontal=�$��RightToLeft=�$��10q�.+��� $��50��$��25�� �� IsCancel . .��Отмена�� -   $��50��$��25��$��10 0 0 0q� �� 	IsDefault . .��Ок