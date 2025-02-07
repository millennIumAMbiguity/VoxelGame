using Zenject;
using UnityEngine;
using VoxelGame.Voxel;
using VoxelGame.Input;

namespace VoxelGame.Core
{
    public class GameInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            BindInput();
            BindVoxel();
            BindMenues();
        }

        private void BindInput()
        {
            Container.Bind<Inputs>().AsSingle();
        }

        private void BindVoxel()
        {
            Container.Bind<IGenerator>().To<DefaultGenerator>().AsSingle();
            Container.Bind<IChunkView>().To<ChunkView>().FromNew().AsTransient().Lazy();
            Container.BindFactory<Vector2Int, Chunk, Chunk.Factory>();
            Container.Bind<ChunkSystem>().FromComponentInHierarchy().AsSingle();
        }

        private void BindMenues()
        {
            Container.Bind<CanvasFader>().FromComponentInHierarchy().AsSingle();
            Container.Bind<SettingsMenu>().FromComponentInHierarchy().AsSingle();
            Container.Bind<PauseMenu>().FromComponentInHierarchy().AsSingle();
            Container.Bind<InventoryMenu>().FromComponentInHierarchy().AsSingle();
            Container.Bind<GameMenu>().FromComponentInHierarchy().AsSingle();
        }
    }
}