using System.Collections.Generic;
using GeonBit.UI.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace GeonBit.UI.Entities {
    
    /// <summary>
    /// HTML Paragraph is a paragraph that can have other elements inserted inside it
    /// The text will flow around it
    /// </summary>
    [System.Serializable] 
    public class HTMLParagraph : Entity {
        //private List<Paragraph> parts = new List<Paragraph>();
        private List<Vector2> offsets = new List<Vector2>();

        /*public Entity AddChild(Entity child) {
            //Entity e = base.AddChild(child);
            Entity e = child;
            Paragraph p = e as Paragraph;
            if (p != null) {
                AddParagraph(p);
            }
            return e;
        }
        private void AddParagraph(Paragraph p) {
            parts.Add(p);
            //CalculateOffsets();
        }*/

        public HTMLParagraph() : base(new Vector2(-1, -1), Anchor.Auto, Vector2.Zero) { //grows infinitely if I don't specify a size?
            
        }
        
        override internal protected void UpdateDestinationRects()
        {

            // do extra preparation for text entities
            CalculateOffsets();
            
            
            // call base function
            base.UpdateDestinationRects();
        }

        public void CalculateOffsets() {
            offsets.Clear();
            if (_children.Count == 0) {
                return;
            }

            Paragraph curr = null;
            Paragraph next = null;
            int i = 0;
            int j = 0;

            while (curr == null && i < _children.Count) {
                curr = _children[i] as Paragraph;
                i++;
            }

            if (curr != null) {
                offsets.Add(Vector2.Zero);
                curr.StartingOffset = offsets[j];
                curr.CalcTextActualRectWithWrap();
                offsets.Add(curr.EndingOffset);
                j++;
            }

            while (curr != null) {
                while (next == null && i < _children.Count) { //skip over things until I put more types in here to flow around. Right now just fancy text.
                    next = _children[i] as Paragraph;
                    i++;
                }

                if (next != null) {
                    next.StartingOffset = offsets[j];
                    next.CalcTextActualRectWithWrap();
                    offsets.Add(next.EndingOffset);
                    j++;
                }
                
                curr = next;
                next = null;
            }
        }
        
        /// <summary>
        /// Draw the entity.
        /// </summary>
        /// <param name="spriteBatch">Sprite batch to draw on.</param>
        /// <param name="phase">The phase we are currently drawing.</param>
        /*override protected void DrawEntity(SpriteBatch spriteBatch, DrawPhase phase)
        {
            MarkAsDirty();
            // update processed text if needed
            //if (_needUpdateStyleInstructions)
            {
                ParseStyleInstructions();
                UpdateDestinationRects();
            }//
            
            
            
            //draw each text entity, continuing from the previous point

            
        }*/
    }
}