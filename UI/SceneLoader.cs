namespace EDNE.UI
{
    /// <summary>
    /// 게임 씬 Enum
    /// UIManager에서 씬 전환 시 타입 안전성을 위해 사용
    /// </summary>
    public enum GameScene
    {
        // 메인 씬들
        Title,
        Play,
        Level_01_Gameplay,
        
        // 개발/테스트 씬들
        Hyunmin,
        JIHUN,
        dohyeon,
        Enemy,
        door_scene,
        Test_pjw,
        
        // 추가 씬 (필요시)
        Level_02_Gameplay,
        Level_03_Gameplay,
        GameOver,
        
        // 새 씬 추가는 여기에!
    }
}

