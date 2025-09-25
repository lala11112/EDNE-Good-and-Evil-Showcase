using UnityEngine;

public class PlayerTrig : MonoBehaviour
{
   int door_top_trig;
   int door_bottom_trig;
   int door_left_trig;

   GameObject door_top;
   GameObject door_bottom;
   GameObject door_left;

   Transform door_top_trans;
   Transform door_bottom_trans;
   Transform door_left_trans;


   void Start()
   {
      door_top_trig=0;
      door_bottom_trig=0;
      door_left_trig=0;

      door_top=GameObject.Find("door1");
      door_bottom=GameObject.Find("door2");
      door_left=GameObject.Find("door3");

      door_top_trans=door_top.transform;
      door_bottom_trans=door_bottom.transform;
      door_left_trans=door_left.transform;

   }

   void OnTriggerEnter2D(Collider other)
   {
      if(other.gameObject.tag=="door1")
      {
         door_top_trig=1;
      }

      if(other.gameObject.tag=="door2")
      {
         door_bottom_trig=1;
      }

      if(other.gameObject.tag=="door3")
      {
         door_left_trig=1;
      }
   }

   void OnTriggerExit2D(Collider other)
   {
      if(other.gameObject.tag=="door1")
      {
         door_top_trig=2;
      }

      if(other.gameObject.tag=="door2")
      {
         door_bottom_trig=2;
      }

      if(other.gameObject.tag=="door3")
      {
         door_left_trig=2;
      }
   }

   void Update()
   {
      if(door_top_trig==1)
      {
         door_top_trans.position=new Vector3.moveTowards(door_top_trans.position,new Vector3(door_top_trans.position.x,door_top_trans.position.y+door_top.GetComponent<boxCollider2D>().size.y,door_top_trans.position.z) * Time.deltaTime,10f);
      }else if(door_top_trig==2){
         door_top_trans.position=new Vector3.moveTowards(door_top_trans.position,new Vector3(door_top_trans.position.x,door_top_trans.position.y-door_top.GetComponent<boxCollider2D>().size.y,door_top_trans.position.z) * Time.deltaTime,10f);
      }

      if(door_bottom_trig==1)
      {
         door_bottom_trans.position=new Vector3.moveTowards(door_bottom_trans.position,new Vector3(door_bottom_trans.position.x,door_bottom_trans.position.y+door_bottom.GetComponent<boxCollider2D>().size.y,door_bottom_trans.position.z) * Time.deltaTime,10f);
      }else if(door_bottom_trig==2){
         door_bottom_trans.position=new Vector3.moveTowards(door_bottom_trans.position,new Vector3(door_bottom_trans.position.x,door_bottom_trans.position.y-door_bottom.GetComponent<boxCollider2D>().size.y,door_bottom_trans.position.z) * Time.deltaTime,10f);
      }


      if(door_left_trig==1)
      {
         door_left_trans.position=new Vector3.moveTowards(door_left_trans.position,new Vector3(door_left_trans.position.x+door_left.GetComponent<boxCollider2D>().size.x,door_left_trans.position.y,door_left_trans.position.z) * Time.deltaTime,10f);
      }else if(door_left_trig==2){
         door_left_trans.position=new Vector3.moveTowards(door_left_trans.position,new Vector3(door_left_trans.position.x-door_left.GetComponent<boxCollider2D>().size.x,door_left_trans.position.y,door_left_trans.position.z) * Time.deltaTime,10f);
      }
   }
    


}
