﻿using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace MCForge
{
    public class CmdGrapple : Command
    {
        public override string name { get { return "grapple"; } }
        public override string shortcut { get { return "g"; } }
        public override string type { get { return "ctf"; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Banned; } }
        public CmdGrapple() { }
        Thread tMain = null;
        Player p = null;

        public void rocketMethod()
        {
            if (p.level.name != Server.pctf.currentLevelName) return;
            if (p.grapple < 1) { p.SendMessage("&8 - " + Server.DefaultColor + "You need to buy grapple at the /store!" + "&8 - "); return; }

            p.grapple = p.grapple - 1;

            Level level = Level.Find(Server.pctf.currentLevelName);
            if (level == null) return;
            string name = p.name;

            ushort by = p.level.GetTile(Convert.ToUInt16(p.pos[0] / 32), Convert.ToUInt16(p.pos[1] / 32), Convert.ToUInt16(p.pos[2] / 32));
            p.SendBlockchange(Convert.ToUInt16(p.pos[0] / 32), Convert.ToUInt16(p.pos[1] / 32), Convert.ToUInt16(p.pos[2] / 32), Block.air);
            Pos bp;
            bp.ending = 1;

            double a = Math.Sin(((double)(128 - p.rot[0]) / 256) * 2 * Math.PI);
            double b = Math.Cos(((double)(128 - p.rot[0]) / 256) * 2 * Math.PI);
            double c = Math.Cos(((double)(p.rot[1] + 64) / 256) * 2 * Math.PI);

            double bigDiag = Math.Sqrt(Math.Sqrt(p.level.width * p.level.width + p.level.height * p.level.height) + p.level.depth * p.level.depth + p.level.width * p.level.width);

            List<CatchPos> previous = new List<CatchPos>();
            List<CatchPos> allBlocks = new List<CatchPos>();
            CatchPos pos;

            Thread gunThread = new Thread(new ThreadStart(delegate
            {
                ushort startX = (ushort)(p.pos[0] / 32);
                ushort startY = (ushort)(p.pos[1] / 32);
                ushort startZ = (ushort)(p.pos[2] / 32);

                pos.x = (ushort)Math.Round(startX + (double)(a * 3));
                pos.y = (ushort)Math.Round(startY + (double)(c * 3));
                pos.z = (ushort)Math.Round(startZ + (double)(b * 3));

                for (double t = 4; bigDiag > t; t++)
                {
                    pos.x = (ushort)Math.Round(startX + (double)(a * t));
                    pos.y = (ushort)Math.Round(startY + (double)(c * t));
                    pos.z = (ushort)Math.Round(startZ + (double)(b * t));

                    by = p.level.GetTile(pos.x, pos.y, pos.z);

                    if (by != Block.air) break;

                    if (by != Block.air && !allBlocks.Contains(pos))
                    {
                        if (p.level.physics < 2 || bp.ending <= 0)
                        {
                            break;
                        }
                        else
                        {
                            if (bp.ending == 1)
                            {
                                if ((!Block.LavaKill(by) && !Block.NeedRestart(by)) && by != Block.glass)
                                {
                                    break;
                                }
                            }
                            else if (p.level.physics >= 3)
                            {
                                if (by != Block.glass)
                                {
                                    level.MakeExplosion(name, Convert.ToUInt16(pos.x - 1), Convert.ToUInt16(pos.y - 1), Convert.ToUInt16(pos.z - 1), 0, false, false, false);
                                    break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    p.level.Blockchange(pos.x, pos.y, pos.z, Block.glass);
                    previous.Add(pos);
                    allBlocks.Add(pos);

                    if (t > 12 && bp.ending != 3)
                    {
                        pos = previous[0];
                        p.level.Blockchange(pos.x, pos.y, pos.z, Block.glass);
                        previous.Remove(pos);
                    }

                    if (bp.ending != 3) Thread.Sleep(20);
                }

                if (bp.ending == -1)
                    try
                    {
                        unchecked { p.SendPos((byte)-1, (ushort)(previous[previous.Count - 3].x * 32), (ushort)(previous[previous.Count - 3].y * 32 + 32), (ushort)(previous[previous.Count - 3].z * 32), p.rot[0], p.rot[1]); }
                    }
                    catch { }
                if (bp.ending == 3) Thread.Sleep(400);

                foreach (CatchPos pos1 in previous)
                {
                    p.level.Blockchange(pos1.x, pos1.y, pos1.z, Block.obsidian);
                    if (bp.ending != 3) Thread.Sleep(20);
                }
            }));
            gunThread.Start();
        }

        public override void Use(Player pp, string message)
        {
            p = pp;
            tMain = new Thread(new ThreadStart(rocketMethod));
            tMain.Start();
        }
        public override void Help(Player p)
        {
            Player.SendMessage(p, "/rocket - shoots a rocket from your face");
        }

        private double Radians(double angle)
        {
            double radians = (Math.PI / 180) * angle;
            Server.s.Log(Convert.ToString("Angle: " + angle + " Radians:" + radians));
            return (radians);
        }

        public static int getUnsigned(int b)
        {
            if (b < 0)
            {
                return b + 256;
            }
            return b;
        }

        public struct CatchPos { public ushort x, y, z; }
        public struct Pos { public ushort x, y, z; public int ending; }
    }
}