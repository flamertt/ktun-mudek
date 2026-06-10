import { AlertTriangle, Calculator, ChevronRight, GraduationCap, Layers, ListChecks, RotateCcw } from 'lucide-react'
import { useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'

import sectionStyles from '@shared/ui/page-section/PageSection.module.css'
import { PageSection } from '@shared/ui/page-section/PageSection.jsx'
import styles from './CourseEvaluationPage.module.css'
import { useCourseEvaluationMudekData } from '../model/useCourseEvaluationMudekData'

export function CourseEvaluationPage() {
  const { offeringId } = useParams()
  const navigate = useNavigate()
  const d = useCourseEvaluationMudekData(offeringId)
  const [confirmReset, setConfirmReset] = useState(false)

  const nav = useMemo(
    () => [
      {
        title: 'Öğrenci sonuçları',
        desc: 'Başarı setleri + en iyi/en kötü/ortalama öğrenci + tablo.',
        to: `/evaluations/${offeringId}/mudek/students`,
      },
      {
        title: 'Sınav özetleri',
        desc: 'Katılımcı ve toplam puan istatistikleri.',
        to: `/evaluations/${offeringId}/mudek/exams`,
      },
      {
        title: 'Soru / bileşen başarıları',
        desc: 'Soru veya bileşen bazında başarı oranı.',
        to: `/evaluations/${offeringId}/mudek/question-components`,
      },
      {
        title: 'DOC → CLO katkı matrisi',
        desc: 'WeightedAchievement (eşleme × başarı).',
        to: `/evaluations/${offeringId}/mudek/item-clo`,
      },
      {
        title: 'CLO sonuçları',
        desc: 'CLO seviyesinde test + anket bileşenleri.',
        to: `/evaluations/${offeringId}/mudek/clo`,
      },
      {
        title: 'Program çıktısı sonuçları',
        desc: 'PÇ seviyesinde nihai başarı skoru.',
        to: `/evaluations/${offeringId}/mudek/program-outcomes`,
      },
    ],
    [offeringId],
  )

  return (
    <PageSection title={d.title} description="Değerlendirme (dashboard)" error={d.error} loading={d.loading}>
      <div className={styles.layout}>
        <p className={styles.headerLine}>
          {d.courseDetail?.termName ?? d.courseDetail?.TermName ? `${d.courseDetail?.termName ?? d.courseDetail?.TermName} · ` : ''}
          {d.courseDetail?.section ?? d.courseDetail?.Section ? `Şube ${d.courseDetail?.section ?? d.courseDetail?.Section} · ` : ''}
          {d.students?.length ? `${d.students.length} kayıtlı öğrenci` : '—'}
        </p>

        <div className={styles.pageGrid}>
          <div className={styles.mainCol}>
            {d.evaluationId ? (
              <section className={styles.examsHero} aria-labelledby="exams-hero-title">
                <div className={styles.examsHeroAccent} aria-hidden />
                <div className={styles.examsHeroInner}>
                  <div className={styles.examsHeroIcon}>
                    <GraduationCap size={28} strokeWidth={2.2} aria-hidden />
                  </div>
                  <div className={styles.examsHeroBody}>
                    <h3 id="exams-hero-title" className={styles.examsHeroTitle}>
                      Sınavlar ve ölçme yapısı
                    </h3>
                    <p className={styles.examsHeroDesc}>
                      Bu bölümde sınavları tanımlayıp ağırlıkları ayarlarsınız. Her sınav için sorular, yazılı soru–DÖÇ
                      eşlemeleri, öğrenci cevapları; ayrıca ölçme bileşenleri, DÖÇ eşlemeleri ve not girişi sayfalarına
                      buradan geçilir.
                    </p>
                    <ul className={styles.examsHeroList}>
                      <li>
                        <ListChecks size={15} className={styles.examsHeroLiIcon} aria-hidden />
                        Sınav listesi → soru yönetimi ve öğrenci cevapları
                      </li>
                      <li>
                        <Layers size={15} className={styles.examsHeroLiIcon} aria-hidden />
                        Aynı sınavda ölçme bileşenleri ve öğrenci notları
                      </li>
                    </ul>
                    <button
                      type="button"
                      className={styles.examsHeroCta}
                      onClick={() => navigate(`/evaluations/${offeringId}/evaluation/${d.evaluationId}/exams`)}
                    >
                      Sınavları yönet
                      <ChevronRight size={20} aria-hidden />
                    </button>
                  </div>
                </div>
              </section>
            ) : null}

            <div className={styles.panel}>
              <h3 className={styles.panelTitle}>MÜDEK tabloları</h3>
              <p className={styles.panelHint}>
                Tablolar ayrı sayfalarda. İlgili karta basarak detay sayfaya gidebilirsin.
              </p>

              <div className={styles.quickGrid}>
                {nav.map((t) => (
                  <div key={t.to} className={styles.quickCard}>
                    <h4 className={styles.quickTitle}>{t.title}</h4>
                    <p className={styles.quickDesc}>{t.desc}</p>
                    <button type="button" className={styles.goBtn} onClick={() => navigate(t.to)}>
                      Tabloya git
                    </button>
                  </div>
                ))}
              </div>
            </div>

            {d.mudekLoading && !d.mudekResults ? (
              <p className={sectionStyles.muted} style={{ marginTop: '0.25rem' }}>
                MÜDEK sonuçları yükleniyor…
              </p>
            ) : null}
          </div>

          <div className={styles.sideCol}>
            <div className={styles.panel}>
              <h3 className={styles.panelTitle}>Durum</h3>
              <p className={styles.summaryLine}>
                Son hesaplama:{' '}
                {d.mudekMeta?.lastCalculatedAt ? d.formatDate(d.mudekMeta.lastCalculatedAt) : '—'}
              </p>

              {/* CLO kaynak kilidi göstergesi */}
              {d.cloDataSource === 'db' ? (
                <div style={{ display: 'flex', gap: '0.4rem', alignItems: 'flex-start', background: 'var(--color-warning-bg, #fffbe6)', border: '1px solid var(--color-warning, #f0b429)', borderRadius: '6px', padding: '0.55rem 0.7rem', margin: '0.5rem 0', fontSize: '0.82rem', color: 'var(--color-warning-text, #7c5e00)' }}>
                  <AlertTriangle size={15} style={{ flexShrink: 0, marginTop: '1px' }} aria-hidden />
                  <span>
                    <strong>Yerel DÖÇ kaynağı kilitli.</strong> Üniversite API'si boştu; hesaplamalar veritabanındaki DÖÇ kayıtlarını kullanıyor.
                    API dolduğunda sıfırla butonuyla kilidi kaldırabilirsin — eşlemeler temizlenir, yeniden yapılması gerekir.
                  </span>
                </div>
              ) : d.cloDataSource === 'api' ? (
                <p className={styles.summaryLine} style={{ fontSize: '0.82rem' }}>
                  DÖÇ kaynağı: <strong>Üniversite API</strong>
                </p>
              ) : null}

              {/* Reset sonucu uyarısı */}
              {d.cloLockResetResult ? (
                <div style={{ display: 'flex', gap: '0.4rem', alignItems: 'flex-start', background: 'var(--color-info-bg, #e8f4fd)', border: '1px solid var(--color-info, #2196f3)', borderRadius: '6px', padding: '0.55rem 0.7rem', margin: '0.5rem 0', fontSize: '0.82rem', color: 'var(--color-info-text, #0d47a1)' }}>
                  <AlertTriangle size={15} style={{ flexShrink: 0, marginTop: '1px' }} aria-hidden />
                  <span>
                    Kaynak kilidi sıfırlandı.{' '}
                    {(d.cloLockResetResult.deletedQuestionMappings ?? 0) > 0
                      ? `${d.cloLockResetResult.deletedQuestionMappings} soru, `
                      : ''}
                    {(d.cloLockResetResult.deletedComponentMappings ?? 0) > 0
                      ? `${d.cloLockResetResult.deletedComponentMappings} bileşen, `
                      : ''}
                    {(d.cloLockResetResult.clearedSurveyQuestionClos ?? 0) > 0
                      ? `${d.cloLockResetResult.clearedSurveyQuestionClos} anket sorusu `
                      : ''}
                    DÖÇ eşlemesi temizlendi.{' '}
                    <strong>Sorularınızı ve anket sorularınızı yeniden DÖÇ ile eşleştirmeniz gerekiyor.</strong>
                  </span>
                </div>
              ) : null}

              {d.mudekMeta?.isCalculationDirty ? (
                <p className={styles.dirtyHint}>
                  Notlar veya yapı değişti; tablolar eski olabilir. Aşağıdan yeniden hesaplayın.
                </p>
              ) : null}

              <button
                type="button"
                className={styles.calcBtn}
                disabled={Boolean(d.loading || !offeringId || d.mudekCalcRunning)}
                onClick={() => void d.runMudekCalculation()}
              >
                <Calculator size={18} aria-hidden />
                {d.mudekCalcRunning ? 'Hesaplanıyor…' : 'MÜDEK sonuçlarını hesapla'}
              </button>

              {/* CLO kaynak sıfırlama */}
              {d.cloDataSource != null && d.evaluationId ? (
                confirmReset ? (
                  <div style={{ marginTop: '0.6rem', padding: '0.6rem 0.75rem', background: 'var(--color-danger-bg, #fff0f0)', border: '1px solid var(--color-danger, #e53935)', borderRadius: '6px', fontSize: '0.82rem' }}>
                    <p style={{ margin: '0 0 0.5rem', color: 'var(--color-danger-text, #b71c1c)', fontWeight: 600 }}>
                      Emin misin? Tüm DÖÇ eşlemeleri silinecek.
                    </p>
                    <div style={{ display: 'flex', gap: '0.5rem' }}>
                      <button
                        type="button"
                        style={{ flex: 1, padding: '0.35rem 0', borderRadius: '5px', border: '1px solid var(--color-danger, #e53935)', background: 'var(--color-danger, #e53935)', color: '#fff', fontWeight: 600, fontSize: '0.82rem', cursor: 'pointer' }}
                        disabled={d.cloLockResetting}
                        onClick={async () => { await d.runResetCloLock(); setConfirmReset(false) }}
                      >
                        {d.cloLockResetting ? 'Sıfırlanıyor…' : 'Evet, sıfırla'}
                      </button>
                      <button
                        type="button"
                        style={{ flex: 1, padding: '0.35rem 0', borderRadius: '5px', border: '1px solid #ccc', background: '#fff', fontSize: '0.82rem', cursor: 'pointer' }}
                        onClick={() => setConfirmReset(false)}
                      >
                        Vazgeç
                      </button>
                    </div>
                  </div>
                ) : (
                  <button
                    type="button"
                    style={{ display: 'flex', alignItems: 'center', gap: '0.4rem', marginTop: '0.5rem', width: '100%', padding: '0.45rem 0.75rem', borderRadius: '6px', border: '1px solid #ccc', background: 'transparent', fontSize: '0.82rem', cursor: 'pointer', color: 'inherit' }}
                    onClick={() => setConfirmReset(true)}
                  >
                    <RotateCcw size={14} aria-hidden />
                    DÖÇ kaynak kilidini sıfırla
                  </button>
                )
              ) : null}

              {d.mudekError ? (
                <p className={sectionStyles.error} role="alert" style={{ margin: '0.4rem 0 0', fontSize: '0.85rem' }}>
                  {d.mudekError}
                </p>
              ) : null}
            </div>

            {d.cloPoMap?.length ? (
              <div className={styles.panel}>
                <h3 className={styles.panelTitle}>CLO ↔ PÇ matrisi (üniversite)</h3>
                <p className={styles.panelHint}>
                  Bu dersin katalog numarasına göre üniversite kaynaklı DÖÇ–program çıktısı ağırlıkları (ilk 15 satır).
                </p>
                <ul style={{ margin: '0.35rem 0 0', paddingLeft: '1.15rem', fontSize: '0.82rem', lineHeight: 1.45 }}>
                  {d.cloPoMap.slice(0, 15).map((row, idx) => {
                    const clo = row.cloId ?? row.CloId
                    const po = row.programOutcomeId ?? row.ProgramOutcomeId
                    const w = row.weight ?? row.Weight
                    return (
                      <li key={`${clo}-${po}-${idx}`}>
                        DÖÇ {clo} → PÇ {po}
                        {w != null ? ` · ağırlık: ${w}` : ''}
                      </li>
                    )
                  })}
                </ul>
                {d.cloPoMap.length > 15 ? (
                  <p className={sectionStyles.muted} style={{ margin: '0.5rem 0 0', fontSize: '0.8rem' }}>
                    … ve {d.cloPoMap.length - 15} kayıt daha
                  </p>
                ) : null}
              </div>
            ) : null}

            {d.evaluation ? (
              <>
                <div className={styles.panel}>
                  <h3 className={styles.panelTitle}>Hızlı erişim</h3>
                  <p className={styles.panelHint}>
                    Harf notu eşikleri lisans programı bazında admin panelinden yönetilir; bu derste program kuralları
                    otomatik uygulanır.
                  </p>
                  <p className={styles.panelHint}>
                    <strong>Sınavlar</strong> ve tüm alt sayfalar için ana sütundaki vurgulu{' '}
                    <strong>Sınavlar ve ölçme yapısı</strong> kutusundaki <strong>Sınavları yönet</strong> düğmesini
                    kullanın.
                  </p>
                </div>

                <div className={styles.panel}>
                  <h3 className={styles.panelTitle}>MÜDEK Snapshot</h3>
                  <p className={styles.panelHint}>
                    Son hesaplanmış MÜDEK değerlendirme sonuçları bu ders açılışı için aşağıda listelenir.
                  </p>
                  <p className={styles.summaryLine}>
                    Snapshot son hesaplama:{' '}
                    {d.mudekMeta?.lastCalculatedAt ? d.formatDate(d.mudekMeta.lastCalculatedAt) : '—'}
                  </p>
                </div>
              </>
            ) : null}
          </div>
        </div>
      </div>
    </PageSection>
  )
}

