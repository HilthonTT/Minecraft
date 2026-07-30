namespace Minecraft.Core.Utilities.Models;

public struct ModelData
{
    /*
     * Model data stored as array of structures.
     */

    public float[] positions; //X, Y, Z
    public float[] normals; //X, Y, Z
    public float[] textureCoordinates; //U, V
    public int[] indices; //Indices for index buffer
}
