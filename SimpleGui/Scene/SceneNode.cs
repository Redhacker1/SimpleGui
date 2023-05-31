using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace SimpleGui.Scene
{
    public class SceneNode : DisposableBase
    {

        List<ushort> childIDs = new List<ushort>();

        protected ushort Identifier;
        public Vector2 Position { get; set; }
        public Vector2 Size { get; set; }

        public Vector2 AbsolutePosition { get; protected set; }
        protected SceneNode Parent { get; set; }
        
        //protected List<WeakReference<SceneNode>> Children { get; set; } = new List<WeakReference<SceneNode>>();
        protected readonly Dictionary<ushort, SceneNode> Children = new Dictionary<ushort, SceneNode>();

        protected override void Dispose(bool disposeManagedResources)
        {
            for (int index = 0; index < childIDs.Count; index++)
            {
                Children.TryGetValue(childIDs[index], out SceneNode child);
                if (child != null)
                {
                    RemoveAndDispose(ref child);   
                }
            }

            Children.Clear();

            base.Dispose(disposeManagedResources);
        }

        public virtual void AddChild(SceneNode item)
        {
            item.Parent = this;
            item.AbsolutePosition = AbsolutePosition + item.Position;



            ushort PotentialID = (ushort)Random.Shared.Next(0, ushort.MaxValue);

            while (childIDs.Contains(PotentialID))
            {
                PotentialID = (ushort)Random.Shared.Next(0, ushort.MaxValue);
            }

            item.Identifier = PotentialID;
            childIDs.Add(PotentialID);
            Children.Add(PotentialID, AddDisposable(item));


        }
        
        

        public virtual void RemoveChild(SceneNode item)
        {
            RemoveToDispose(item);
            Children.Remove(item.Identifier);
            
            
            item.Parent = null;
            item.Identifier = 0;
        }

        public void RemoveSelfFromParent()
        {
            Parent?.RemoveChild(this);
        }
        
        

        public virtual void Update()
        {
            AbsolutePosition = Position;
            if (Parent != null)
            {
                AbsolutePosition += Parent.AbsolutePosition;
            }

            for (int i = 0; i < childIDs.Count; i++)
            //foreach (var item in Children)
            {
                //item.Update();
                Children.TryGetValue(childIDs[i], out SceneNode child);
                child?.Update();
            }
        }

        public virtual void Draw()
        {
            for (int i = 0; i < childIDs.Count; i++)
            {
                Children.TryGetValue(childIDs[i], out SceneNode child);
                child?.Draw();
            }
        }
    }
}
