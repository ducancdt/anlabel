# Quy dinh cho agent

Bat ky agent nao vao lam viec tren du an nay phai thuc hien cac buoc sau truoc khi ket thuc:

0. Moi khi nhan yeu cau tu nguoi dung, phai dua ra plan ngan gon truoc khi lam va thuc hien theo plan do. Neu phat sinh thay doi huong xu ly, phai cap nhat lai plan.
1. Chay kiem tra/test phu hop sau khi sua code. Toi thieu can chay build hoac test lien quan den phan vua thay doi; neu khong chay duoc thi phai ghi ro ly do va loi gap phai trong cau tra loi cuoi.
2. Cap nhat version moi vao phan hien thi tren cua so ung dung sau moi lan thay doi code. Version hien thi phai tang so de nguoi dung nhan biet ban build moi.
3. Khong duoc bao hoan thanh neu chua noi ro da chay lenh kiem tra nao va ket qua ra sao.
4. Luon mac dinh dinh huong san pham la PM cho may in tem nhan cong nghiep, khong phai may in van phong thong thuong. Moi quyet dinh lien quan den printer setup, driver, paper size, orientation, DPI, calibration, print preview va print pipeline phai uu tien hanh vi thuc te cua cac dong may nhu Zebra, TSC, Godex, SATO, Argox, Honeywell, Intermec, Citizen, Toshiba TEC va cac driver Seagull/BarTender.
5. Khi lam viec voi kho giay may in, khong duoc gia dinh `PrintCapabilities.PageMediaSizeCapability` la du. Phai uu tien doc thong tin kho giay/driver theo huong hop voi may in tem nhan cong nghiep; neu WPF `PrintCapabilities` khong du thi phai can nhac fallback nhu `System.Drawing.Printing.PrinterSettings.PaperSizes`, DEVMODE/driver preferences, hoac cho nguoi dung nhap tay. Khong quay lai danh sach kho giay hardcode cua phan mem tru khi nguoi dung yeu cau ro rang.
6. Neu co xung dot giua cach lam dung cho may in van phong va cach lam dung cho may in tem nhan cong nghiep, uu tien cach lam dung cho may in tem nhan cong nghiep.
