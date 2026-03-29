import { useParams } from 'react-router-dom';
import { useBooking } from '../../hooks/useBooking';
import { RegisterStep }       from './steps/RegisterStep';
import { AvailabilityStep }   from './steps/AvailabilityStep';
import { ConfirmStep }        from './steps/ConfirmStep';
import { MyReservationsStep } from './steps/MyReservationsStep';
import styles from './BookingPage.module.scss';

export const BookingPage = () => {
  const { tenantId = '' } = useParams<{ tenantId: string }>();
  const {
    step, loading, error, clearError,
    selectedDate, setSelectedDate,
    slots, setSlots, setSelectedSlot,
    selectedSlot, notes, setNotes,
    reservationId, reservations,
    register, loadSlots, createReservation, loadReservations, cancelReservation,
    resetAvailability,
  } = useBooking(tenantId);

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        <div className={styles.header}>
          <div className={styles.logo}>
            <span className={styles.logoIcon}>N</span>
            <span>NexaFlow Booking</span>
          </div>
          <p>Reserva tu cita de forma rápida y sencilla</p>
        </div>

        {step === 'register' && (
          <RegisterStep loading={loading} error={error} onRegister={register} onClearError={clearError} />
        )}

        {step === 'availability' && (
          <AvailabilityStep
            loading={loading} error={error}
            selectedDate={selectedDate} slots={slots}
            selectedSlot={selectedSlot} notes={notes}
            onDateChange={date => { setSelectedDate(date); setSlots([]); setSelectedSlot(''); }}
            onLoadSlots={loadSlots}
            onSelectSlot={setSelectedSlot}
            onNotesChange={setNotes}
            onReserve={createReservation}
            onViewReservations={loadReservations}
          />
        )}

        {step === 'confirm' && (
          <ConfirmStep
            reservationId={reservationId}
            selectedDate={selectedDate}
            selectedSlot={selectedSlot}
            onViewReservations={loadReservations}
            onNewReservation={resetAvailability}
          />
        )}

        {step === 'my-reservations' && (
          <MyReservationsStep
            reservations={reservations}
            loading={loading} error={error}
            onCancel={cancelReservation}
            onBack={resetAvailability}
          />
        )}
      </div>
    </div>
  );
};
