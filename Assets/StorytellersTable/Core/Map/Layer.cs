using UnityEngine;

namespace Assets.StorytellersTable.Core.Map
{
    public class Layer
    {
        public int Val { get; private set; }

        public Layer(int layer)
        {
            Val = layer;
        }

        /// <summary>
        /// Return's the y-position of this layer. (Position of tile surface visual on this layer).
        /// </summary>
        /// <returns></returns>
        public int Y()
        {
            //return this.Val * Singleton.Instance.height;
            return (int)(this.Val * Singleton.Instance.height + (Singleton.Instance.height / 2f));
        }

        public override int GetHashCode()
        {
            return Val;
        }

        public int CompareTo(Layer layer)
        {
            if (layer is null)
                return 1;
            return this.Val.CompareTo(layer.Val);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Layer);
        }

        public bool Equals(Layer other)
        {
            if (other is null) 
                return false;
            if (ReferenceEquals(this, other)) 
                return true;
            return Val == other.Val;
        }

        /// <summary>
        /// Converts a y-position, <paramref name="yPos"/>, to the closest layer.
        /// </summary>
        /// <param name="yPos"></param>
        /// <returns></returns>
        public static Layer YToLayer(float yPos)
        {
            return new Layer((int)Mathf.Round(yPos / Singleton.Instance.height));
        }

        public static Layer operator +(Layer a, Layer b)
        {
            return new Layer(a.Val + b.Val);
        }

        public static Layer operator -(Layer a, Layer b)
        {
            return new Layer(a.Val - b.Val);
        }

        public static Layer operator *(Layer a, Layer b)
        {
            return new Layer(a.Val * b.Val);
        }

        public static bool operator ==(Layer a, Layer b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Layer a, Layer b)
        {
            return !(a == b);
        }

        public static bool operator >(Layer a, Layer b)
        {
            return (a.Val > b.Val);
        }

        public static bool operator <(Layer a, Layer b)
        {
            return (a.Val < b.Val);
        }
    }
}
