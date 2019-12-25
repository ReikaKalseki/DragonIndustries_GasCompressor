using System.Collections.Generic;
using System;
using VRageMath;

using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.ParticleEffects;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems;
using Sandbox.Game;
using Sandbox.Definitions;

using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Library.Utils;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace DragonIndustriesCompressors {
	
	[MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
	public class ExplosionHandler : MySessionComponentBase {
		
	    private bool registered = false;
	    private readonly MyRandom rand = new MyRandom();
    
		public override void UpdateBeforeSimulation(){
			if (MyAPIGateway.Session == null)
				return;
			if (!registered) {
				register();
			}
		}

		//public override void UpdateBeforeSimulation() {
	   // 	base.UpdateBeforeSimulation();
	    //	MyAPIGateway.Utilities.ShowNotification("On tick", 15, MyFontEnum.Red);
		//}
    
		private void register() {
			bool sim = MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE || MyAPIGateway.Multiplayer.IsServer;
			if (sim)
				MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(1000, onEntityDamaged);
			registered = true;
			//MyAPIGateway.Utilities.ShowNotification("Registered", 50000, MyFontEnum.Red);
		}

		private void onEntityDamaged(object entity, ref MyDamageInformation info) {
			if (entity is IMySlimBlock) {
				IMySlimBlock slim = entity as IMySlimBlock;
				if (slim.FatBlock is IMyGasTank) {
					IMyGasTank tank = slim.FatBlock as IMyGasTank;
					MyCubeBlockDefinition def = slim.BlockDefinition as MyCubeBlockDefinition;
					//MyAPIGateway.Utilities.ShowNotification(def.Id.SubtypeName, 5000, MyFontEnum.Red);
					bool isCompressor = def.Id.SubtypeName.EndsWith("ReikaCompressor", StringComparison.InvariantCultureIgnoreCase);
					float f = (float)tank.FilledRatio;
					//MyAPIGateway.Utilities.ShowNotification(f*100+"%", 5000, MyFontEnum.Red);
					if (f >= 0.1 && isCompressor) {
						float thresh = getFunctionalityThreshold(slim, def, ref info);
						float health = getCurrentHealthFraction(slim, def, ref info);
						//MyAPIGateway.Utilities.ShowNotification(health+" & "+thresh, 5000, MyFontEnum.Red);
						if (health < thresh) {
							bool hydrogen = def.Id.SubtypeName.ToLowerInvariant().Contains("hydrogen");
							float dmg = 90000*f;
							float r = 16*f;
							if (hydrogen) {
								r *= 1.5F;
								dmg *= 2.5F;
							}
							//MyAPIGateway.Utilities.ShowNotification(dmg+" "+r+" from "+f+" ["+hydrogen+"]", 5000, MyFontEnum.Red);
							BoundingSphereD sphere = new BoundingSphereD(tank.GetPosition(), r);
							MyExplosionTypeEnum type = hydrogen ? MyExplosionTypeEnum.BOMB_EXPLOSION : MyExplosionTypeEnum.AMMO_EXPLOSION;
							MyExplosionInfo ex = new MyExplosionInfo(dmg*3, dmg, sphere, type, true);
							ex.CreateParticleEffect = true;
							ex.LifespanMiliseconds = 150+(int)(r*45);
							//ex.StrengthImpulse *= 1.5F;
							ex.CreateShrapnels = true;
							ex.CreateDebris = true;
							//ex.AffectVoxels = true;
							//ex.KeepAffectedBlocks = false;
							//ex.OriginEntity = tank.EntityId;
							MyExplosions.AddExplosion(ref ex, true);
						}
					}
				}
			}
		}
		
		private float getFunctionalityThreshold(IMySlimBlock slim, MyCubeBlockDefinition def, ref MyDamageInformation info) {
			if (info.Type == MyDamageType.Grind) {
				float thresh = def.CriticalIntegrityRatio*slim.MaxIntegrity;
				return thresh;
			}
			else {
				float thresh = def.CriticalIntegrityRatio;
				//thresh = (1-thresh);//*slim.MaxIntegrity;
				return thresh;
			}
		}
		
		private float getCurrentHealthFraction(IMySlimBlock slim, MyCubeBlockDefinition def, ref MyDamageInformation info) {
			if (info.Type == MyDamageType.Grind) {
				return slim.BuildIntegrity-info.Amount*slim.MaxIntegrity/100F;
			}
			else {
				float curDmg = (info.Amount+slim.CurrentDamage)/slim.MaxIntegrity;
				return 1-curDmg;
			}
		}
  }
}
