   M S B A M L   `   `   `  ����   BoardClient    BoardClient.SettingWindow   W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     X TXceed.Wpf.Toolkit, Version=2.1.0.0, Culture=neutral, PublicKeyToken=3e4669d2f30244f44xctk)http://schemas.xceed.com/wpf/xaml/toolkit    � Title$  Настройки��$	��320��$	��400�� � WindowStyle$ 
ToolWindow=� � 
ResizeMode$ NoResize=� � SizeToContent$ Height=� � WindowStartupLocation$ CenterOwner=� � ShowInTaskbar .   � 	Resources  �� 
TargetType �� Property	 �� Value��%/   '��       �� " ��.���  � ��$
	 Bold"�.��� 
 � RowDefinitions
 �� $��40 ��� $��40 ��� $��40 ��� $��40 ��� $��40 ��� $��40 ��� $��40 � � ColumnDefinitions �� $	��130 ��� .+��� $��0��$��Center=�$��5q�Адрес сервера�� -   $��spAdress��$��0��$��1�� �� Orientation$ 
Horizontal=�.+��� $��Center=�$��5 0 0 0q�IP:�� -   $
��tbIp��$	��100��$��8q� � ToolTip$D >Ip адрес сервера в формате 255.255.255.255�� �� 	MaxLength$ 15��$��Center=�$��Center=��� $��Center=�$��5 0 0 0q�	Порт:�� -   $��tbPort��$��50��$��8q�$# Порт от 1024 до 65535��$ 5��$��Center=�$��Center=��� $��1��$��Center=�$��5q�Прозрачность�� -   $��	slOpacity��$��1��$��1��$��5q� � Minimum$	 0.1Q� � Maximum$	 1.0Q�$��Center=� � LargeChange$
 0.05Q� � SmallChange$
 0.01Q� �� TickPlacement$ BottomRight=� �� TickFrequency$
 0.05Q� �� AutoToolTipPrecision$ 1�� �� Delay$	 100�� �� IsSnapToTickEnabled .  �� *Value+ �� RelativeSource �� *Self+�� $��2��$��Center=�$��5q�Положение�� -   $��	cbTopmost��$��2��$��1��$��Center=�$��5q�.��Поверх окон�� $��3��$��Center=�$��5q�%#Частота обновления�� -   $��slUpdateRate��$��3��$��1��$��5q�$ 50Q�$
 1000Q�$��Center=�$	 100Q�$ 10Q�$ BottomRight=�$ 10Q�$ 1��$	 100�� .  �� *Value+ �� *Self+�� $��4��$��Center=�$��5q�Цвет текста#  Xceed.Wpf.Toolkit.ColorPicker  -     Name$ cpText��$��4��$��1��$	��220��$
��Left=�$��	10 5 10 5q��� $��5��$��Center=�$��5q�Цвет фона  -   $ cpBackground��$��5��$��1��$	��220��$
��Left=�$��	10 5 10 5q��� $��1��$��6��$ 
Horizontal=�$��RightToLeft=�$��0 0 10 0q�.+��� -	   $��btCancel��$��50��$��25�� �� IsCancel . .��Отмена�� -
   $
��btOk��$��50��$��25�� �� 	IsDefault . $��10 0 0 0q�.��Ок