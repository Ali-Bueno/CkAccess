using System;

[Flags]
public enum ConnectionAndDirection
{
	TR_Up = 1,
	TR_Right = 2,
	BR_Right = 4,
	BR_Down = 8,
	BL_Down = 0x10,
	BL_Left = 0x20,
	TL_Left = 0x40,
	TL_Up = 0x80
}
