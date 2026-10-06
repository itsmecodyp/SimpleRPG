
namespace SimpleRPG
{
    public class NarrativeEvent
    {
        public Actor targetActor;
        public WorldTile targetTile;
        public float duration = 4f;
        public bool shouldPause;
        public float targetZoom;
        public string textToShow;
        public bool actorSaysText = false;

        public NarrativeEvent(Actor targetActor = null, WorldTile targetTile = null, bool shouldPause = false, string textToShow = null, bool actorSaysText = false, float duration = 3f, float targetZoom = 15f)
        {
            this.targetActor = targetActor;
            this.targetTile = targetTile;
            this.duration = duration;
            this.shouldPause = shouldPause;
            this.targetZoom = targetZoom;
            this.textToShow = textToShow;
            this.actorSaysText = actorSaysText;
        }
        public Actor cachedActor;
        public void StartEvent()
        {
            if (shouldPause)
            {
                Config.paused = true;
            }
            if(SimpleRPG.currentActor != null)
            {
                cachedActor = SimpleRPG.currentActor;

                ControllableUnit._unit_main = null;
                ControllableUnit._units.Clear();
                World.world.selected_buttons.unselectAll();
                //mark as true to prevent decisions being made even when not really in control
                cachedActor.addStatusEffect(AssetManager.status.get("possessed"), duration,  false);
            }
            World.world.move_camera.clearFocus();
            if (targetActor != null)
            {
                FocusCamera(targetActor);
            }
            if(targetTile != null)
            {
                FocusCamera(targetTile);
                FlashTile();
            }
            if (textToShow != null)
            {
                if(actorSaysText && targetActor != null)
                {
                    if (SimpleRPG.hasMessagesMod)
                    {
                        if (SimpleRPG.ActorSay != null)
                        {
                            SimpleRPG.ActorSay(targetActor, textToShow, duration - 1);
                        }
                        //Messages.ActorSay(targetActor, textToShow, duration - 1);
                    }
                    else
                    {
                        WorldTip.showNowTop(textToShow, false);
                    }
                    return;
                }
                WorldTip.showNowTop(textToShow, false);
            }
        }

        public void FlashTile()
        {
            MapBox.instance.flash_effects.flashPixel(targetTile, 20, ColorType.White);
        }

        public void EndEvent()
        {
            MoveCamera.clearFocusUnitOnly();
            if (shouldPause)
            {
                Config.paused = false;
            }
            if(cachedActor != null)
            {//give control back to cached unit
                ControllableUnit.setControllableCreature(cachedActor);
                cachedActor = null;
            }
        }

        public void FocusCamera(WorldTile targetTile = null)
        {
            World.world.move_camera._focus_timer = duration;
            World.world.move_camera.focusOn(targetTile.posV3);
            World.world.move_camera._target_zoom = targetZoom;
        }

        public void FocusCamera(Actor targetActor = null)
        {
            World.world.move_camera._focus_timer = duration;
            MoveCamera._focus_unit = targetActor;
            World.world.move_camera._target_zoom = targetZoom;
        }
    }
}
