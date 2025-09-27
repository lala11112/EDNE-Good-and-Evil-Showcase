using UnityEngine;
using UnityEngine.UI;

public class PlayerTrig : MonoBehaviour
{
   int door_top_trig;
   int door_bottom_trig;
   int door_left_trig;



   GameObject door_top;
   GameObject door_bottom;
   GameObject door_left;

   Color door_top_color;
   Color door_bottom_color;
   Color door_left_color;
   



   void Start()
   {
      door_top_trig=0;
      door_bottom_trig=0;
      door_left_trig=0;



      door_top=GameObject.Find("door1");
      door_bottom=GameObject.Find("door2");
      door_left=GameObject.Find("door3");

      door_top_color=door_top.GetComponent<SpriteRenderer>().color;
      door_bottom_color=door_bottom.GetComponent<SpriteRenderer>().color;
      door_left_color=door_left.GetComponent<SpriteRenderer>().color;   
      Invoke("pipe_1to2_open",5f);

   }

   void OnTriggerEnter2D(Collider2D other)
   {
      if(other.gameObject.name=="door1")
      {
         // 현재 위치 기준으로 목표를 문 너비만큼 오른쪽으로 설정 (월드 단위 사용)
         door_top_trig=1;
      }

      if(other.gameObject.name=="door2")
      {
         // 현재 위치 기준으로 목표를 문 너비만큼 오른쪽으로 설정 (월드 단위 사용)
         door_bottom_trig=1;
      }

      if(other.gameObject.name=="door3")
      {
         // 현재 위치 기준으로 목표를 문 높이만큼 위로 설정 (월드 단위 사용)
       
         door_left_trig=1;
      }
   }

   void OnTriggerExit2D(Collider2D other)
   {
      // Exit 동안에는 즉시 닫지 않고, 열린 목표 도달 후 닫히도록 플래그만 설정
      if(other.gameObject.name=="door1")
      {
    
         door_top_trig=2;
      }
      if(other.gameObject.name=="door2")
      {
    
         door_bottom_trig=2;
      }
      if(other.gameObject.name=="door3")
      {
      
         door_left_trig=2;
      }
   }

public void pipe_1to2_open(){
   GameObject.Find("r_1_2_pipe").GetComponent<BoxCollider2D>().isTrigger=true;
}


   void Update()
   {
      gameObject.transform.position = GameObject.Find("Player").transform.position;
      if(door_top_trig==1)
      {
         if(door_top_color.a<=0){
            door_top_color=new Color(0,0,0,0);
         }else{
            door_top_color+=new Color(0,0,0,-3f)*Time.deltaTime;
         }
         door_top.GetComponent<SpriteRenderer>().color=door_top_color;
         
      }else if(door_top_trig==2){
         if(door_top_color.a>=1){
              door_top_color=new Color(0,0,0,1);
           
         }else{
           door_top_color+=new Color(0,0,0,3f)*Time.deltaTime;
         }

         door_top.GetComponent<SpriteRenderer>().color=door_top_color;
      }

      if(door_bottom_trig==1)
      {
         if(door_bottom_color.a<=0){
            door_bottom_color=new Color(0,0,0,0);
         }else{
            door_bottom_color+=new Color(0,0,0,-3f)*Time.deltaTime;
         }
         door_bottom.GetComponent<SpriteRenderer>().color=door_bottom_color;
      }else if(door_bottom_trig==2){
            if(door_bottom_color.a>=1){
            door_bottom_color=new Color(0,0,0,1);
         }else{
            door_bottom_color+=new Color(0,0,0,3f)*Time.deltaTime;
         }
        
         door_bottom.GetComponent<SpriteRenderer>().color=door_bottom_color;

      }


      if(door_left_trig==1)
      {
         if(door_left_color.a<=0){
            door_left_color=new Color(0,0,0,0);
         }else{
            door_left_color+=new Color(0,0,0,-3f)*Time.deltaTime;
         }
         door_left.GetComponent<SpriteRenderer>().color=door_left_color;   
      }else if(door_left_trig==2){
         if(door_left_color.a>=1){
            door_left_color=new Color(0,0,0,1);
         }else{
            door_left_color+=new Color(0,0,0,3)*Time.deltaTime;
         }
         door_left.GetComponent<SpriteRenderer>().color=door_left_color;
        
      }
       }
                


}
