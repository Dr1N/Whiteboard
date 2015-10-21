# Whiteboard
Interactive whiteboard(C#, WCF, WPF).
Language C#. Platform - .Net 4.5.

Interactive Whiteboard: Server - many client.
Server - Teacher board (editor).
Client - Student board (listener).
Configuration - .config files or setting windows.

Solution:
- BoardClient - Student application, listening to the teacher board.
- BoardEditor - Teacher application, edits and translates board (WCF service)
- BoardControls - Сommon controls

Functions:
- translation of text and graphics
- scalable board
- top windows mode
- rich settings
- and more...
