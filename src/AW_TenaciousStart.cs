﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NewTerra
{
    public class AW_TenaciousStart : UpdatableAndDeletable
    {

        private int timer;
        public AW_TenaciousStart(Room room) 
        {
            this.timer = 0;
            this.room = room;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            room.game.cameras[0].MoveCamera(room, 0);
            AbstractCreature firstAlivePlayer = room.game.FirstAlivePlayer;
            if (room.game.session is StoryGameSession && room.game.Players.Count > 0 && firstAlivePlayer != null && firstAlivePlayer.realizedCreature != null && firstAlivePlayer.realizedCreature.room == room && room.game.GetStorySession.saveState.cycleNumber == 0)
            {
                Player player = firstAlivePlayer.realizedCreature as Player;
                player.SuperHardSetPosition(new Vector2(340f, 3120f));
                if (timer > 40)
                {
                    
                    player.mainBodyChunk.vel = new Vector2(0f, -10f);
                    player.Stun(160);
                    this.Destroy();
                    
                }
                
            }
            this.timer++;
        }

    }

    
}
